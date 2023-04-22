using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets.V5;

public sealed class SubscribePacket : MqttPacketWithId
{
    private readonly IReadOnlyList<(ReadOnlyMemory<byte> Filter, byte QoS)> filters;

    public SubscribePacket(ushort id, IReadOnlyList<(ReadOnlyMemory<byte> Filter, byte QoS)> filters) : base(id)
    {
        Verify.ThrowIfNullOrEmpty(filters);

        this.filters = filters;
    }

    public IReadOnlyList<(ReadOnlyMemory<byte> Filter, byte QoS)> Filters => filters;

    protected override byte Header => SubscribeMask;

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out uint subscriptionId,
        out IReadOnlyList<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> userProperties,
        out IReadOnlyList<(byte[], byte)> filters)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            span = span.Slice(0, length);
            id = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            if (!TryReadMqttVarByteInteger(span, out var propLen, out var consumed) ||
                !TryReadProperties(span.Slice(consumed, propLen), out subscriptionId, out userProperties))
            {
                goto ret_false;
            }

            span = span.Slice(consumed + propLen);

            var list = new List<(byte[], byte)>();
            while (!span.IsEmpty)
            {
                if (TryReadMqttString(span, out var filter, out var len) && len < span.Length)
                {
                    list.Add((filter, span[len]));
                    span = span.Slice(len + 1);
                }
                else
                {
                    goto ret_false;
                }
            }

            filters = list;
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence.Slice(0, length));

            if (!reader.TryReadBigEndian(out short local))
                goto ret_false;

            if (!TryReadMqttVarByteInteger(ref reader, out var propLen) ||
                !TryReadProperties(sequence.Slice(reader.Consumed, propLen), out subscriptionId, out userProperties))
            {
                goto ret_false;
            }

            reader.Advance(propLen);

            var list = new List<(byte[], byte)>();

            while (!reader.End)
            {
                if (TryReadMqttString(ref reader, out var filter) && reader.TryRead(out var qos))
                {
                    list.Add((filter, qos));
                }
                else
                {
                    goto ret_false;
                }
            }

            id = (ushort)local;
            filters = list;
            return true;
        }

    ret_false:
        id = 0;
        filters = null;
        subscriptionId = 0;
        userProperties = null;
        return false;
    }

    private static bool TryReadProperties(ReadOnlySpan<byte> span, out uint subscriptionId,
        out IReadOnlyList<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> userProperties)
    {
        userProperties = null;
        subscriptionId = 0;
        List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> props = null;

        while (!span.IsEmpty)
        {
            switch (span[0])
            {
                case 0x0B:
                    if (!TryReadMqttVarByteInteger(span.Slice(1), out var i32, out var count))
                        return false;
                    subscriptionId = (uint)i32;
                    span = span.Slice(count + 1);
                    break;
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out count))
                        return false;
                    span = span.Slice(count + 1);
                    if (!TryReadMqttString(span, out var value, out count))
                        return false;
                    span = span.Slice(count);
                    (props ??= new()).Add(new(key, value));
                    break;
                default:
                    return false;
            }
        }

        userProperties = props;
        return true;
    }

    private static bool TryReadProperties(in ReadOnlySequence<byte> sequence, out uint subscriptionId,
        out IReadOnlyList<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> userProperties)
    {
        userProperties = null;
        subscriptionId = 0;
        List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> props = null;
        var reader = new SequenceReader<byte>(sequence);

        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x0B:
                    if (!TryReadMqttVarByteInteger(ref reader, out var i32))
                        return false;
                    subscriptionId = (uint)i32;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out var value))
                        return false;
                    (props ??= new()).Add(new(key, value));
                    break;
                default:
                    return false;
            }
        }

        userProperties = props;
        return true;
    }

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength) => throw new NotImplementedException();

    public override void Write(Span<byte> span, int remainingLength) => throw new NotImplementedException();

    #endregion
}