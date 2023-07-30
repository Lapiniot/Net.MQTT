using static System.Globalization.CultureInfo;
using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt;

public static class MqttPacketHelpers
{
    [Conditional("DEBUG")]
    public static void DebugDump<TPacket>(this TPacket packet) where TPacket : IMqttPacket
    {
        ArgumentNullException.ThrowIfNull(packet);
        var writer = new ArrayBufferWriter<byte>();
        var written = packet.Write(writer, out var span);
        Debug.WriteLine($"{{{string.Join(",", span.Slice(written).ToArray().Select(b => "0x" + b.ToString("x2", InvariantCulture)))}}}");
    }

    public static async ValueTask<PacketReadResult> ReadPacketAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (SequenceExtensions.TryReadMqttHeader(in buffer, out var flags, out var length, out var offset))
            {
                var total = offset + length;
                if (buffer.Length >= total)
                {
                    return new(flags, offset, length, buffer.Slice(0, total));
                }
            }
            else if (buffer.Length >= 5)
            {
                // We must stop here, because no valid MQTT packet header
                // was found within 5 (max possible header size) bytes
                MalformedPacketException.Throw();
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }
}

public readonly record struct PacketReadResult(byte Flags, int Offset, int Length, ReadOnlySequence<byte> Buffer);