using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets;

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

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<(byte[], byte)> filters)
    {
        var span = sequence.FirstSpan;
        if (span.Length >= length)
        {
            span = span.Slice(0, length);
            id = BP.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            var list = new List<(byte[], byte)>();
            while (span.Length > 0)
            {
                if (SPE.TryReadMqttString(in span, out var filter, out var len) && len < span.Length)
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
        else if (sequence.Length >= length)
        {
            var reader = new SequenceReader<byte>(sequence.Slice(0, length));

            if (!reader.TryReadBigEndian(out short local))
            {
                goto ret_false;
            }

            var list = new List<(byte[], byte)>();

            while (!reader.End)
            {
                if (SRE.TryReadMqttString(ref reader, out var filter) && reader.TryRead(out var qos))
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
        return false;
    }

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;
        for (var i = 0; i < filters.Count; i++)
        {
            remainingLength += filters[i].Filter.Length + 3;
        }

        return 1 + ME.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = SubscribeMask;
        span = span.Slice(1);
        span = span.Slice(SPE.WriteMqttLengthBytes(ref span, remainingLength));
        BP.WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);

        for (var i = 0; i < filters.Count; i++)
        {
            var (filter, qos) = filters[i];
            span = span.Slice(SPE.WriteMqttString(ref span, filter.Span));
            span[0] = qos;
            span = span.Slice(1);
        }
    }

    #endregion
}