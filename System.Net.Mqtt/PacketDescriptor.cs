using System.Net.Mqtt.Packets.V3;
using System.Runtime.InteropServices;

namespace System.Net.Mqtt;

[StructLayout(LayoutKind.Explicit)]
internal readonly struct PacketDescriptor
{
    [FieldOffset(0x00)] private readonly IMqttPacket _packet;
    [FieldOffset(0x00)] private readonly ReadOnlyMemory<byte> _topic;
    [FieldOffset(0x10)] private readonly ReadOnlyMemory<byte> _payload;
    [FieldOffset(0x20)] private readonly uint _raw;

    public PacketDescriptor(IMqttPacket packet) => _packet = packet;

    public PacketDescriptor(uint value) => _raw = value;

    public PacketDescriptor(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags)
    {
        _topic = topic;
        _payload = payload;
        _raw = flags;
    }

    public readonly int WriteTo(PipeWriter output, out byte packetType)
    {
        int size;
        var raw = _raw;

        if ((raw & 0xFF00_0000) is not 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw);
            size = 4;
            packetType = (byte)(raw >>> 28);
        }
        else if (_topic is { Length: not 0, Span: var topic })
        {
            // Decomposed PUBLISH packet
            size = PublishPacket.Write(output, flags: (byte)raw, id: (ushort)(raw >>> 8), topic, _payload.Span);
            packetType = (byte)PacketType.PUBLISH;
            goto ret_skip_advance;
        }
        else if (_packet is { } packet)
        {
            // Reference to any generic packet implementation
            size = packet.Write(output, out var span);
            packetType = (byte)(span[0] >>> 4);
            goto ret_skip_advance;
        }
        else if (raw is not 0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), (ushort)raw);
            size = 2;
            packetType = (byte)(raw >>> 12);
        }
        else
        {
            ThrowHelpers.ThrowInvalidDispatchBlock();
            packetType = 0;
            return 0;
        }

        output.Advance(size);

    ret_skip_advance:
        return size;
    }
}