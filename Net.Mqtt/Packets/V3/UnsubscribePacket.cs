using static Net.Mqtt.PacketFlags;
using SequenceReaderExtensions = Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace Net.Mqtt.Packets.V3;

public sealed class UnsubscribePacket : MqttPacketWithId, IMqttPacket
{
    private readonly IReadOnlyList<ReadOnlyMemory<byte>> filters;

    public UnsubscribePacket(ushort id, IReadOnlyList<ReadOnlyMemory<byte>> filters) : base(id)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentOutOfRangeException.ThrowIfZero(filters.Count);

        this.filters = filters;
    }

    public IReadOnlyList<ReadOnlyMemory<byte>> Filters => filters;

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<byte[]> filters)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            span = span.Slice(0, length);
            id = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            var list = new List<byte[]>();
            while (span.Length > 0)
            {
                if (SpanExtensions.TryReadMqttString(span, out var filter, out var consumed))
                {
                    list.Add(filter);
                    span = span.Slice(consumed);
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

            var list = new List<byte[]>();

            while (!reader.End)
            {
                if (SequenceReaderExtensions.TryReadMqttString(ref reader, out var filter))
                {
                    list.Add(filter);
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
        return false;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer)
    {
        var remainingLength = 2;

        var count = filters.Count;
        for (var i = 0; i < count; i++)
        {
            remainingLength += filters[i].Length + 2;
        }

        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
        var span = writer.GetSpan(size);

        span[0] = UnsubscribeMask;
        span = span.Slice(1);
        SpanExtensions.WriteMqttVarByteInteger(ref span, remainingLength);
        BinaryPrimitives.WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);
        for (var i = 0; i < count; i++)
        {
            SpanExtensions.WriteMqttString(ref span, filters[i].Span);
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}