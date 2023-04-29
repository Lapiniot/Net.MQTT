using static System.Net.Mqtt.PacketFlags;
using SequenceReaderExtensions = System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets.V3;

public sealed class UnsubscribePacket : MqttPacketWithId
{
    private readonly IReadOnlyList<ReadOnlyMemory<byte>> filters;

    public UnsubscribePacket(ushort id, IReadOnlyList<ReadOnlyMemory<byte>> filters) : base(id)
    {
        Verify.ThrowIfNullOrEmpty(filters);

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

    #region Overrides of MqttPacketWithId

    public override int Write(IBufferWriter<byte> writer, out Span<byte> buffer)
    {
        var remainingLength = 2;

        for (var i = 0; i < filters.Count; i++)
        {
            remainingLength += filters[i].Length + 2;
        }

        var size = 1 + MqttExtensions.GetVarBytesCount(remainingLength) + remainingLength;
        var span = buffer = writer.GetSpan(size);

        span[0] = UnsubscribeMask;
        span = span.Slice(1);
        SpanExtensions.WriteMqttVarByteInteger(ref span, remainingLength);
        BinaryPrimitives.WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);
        for (var i = 0; i < filters.Count; i++)
        {
            SpanExtensions.WriteMqttString(ref span, filters[i].Span);
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}