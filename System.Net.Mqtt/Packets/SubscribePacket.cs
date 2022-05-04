using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets;

public sealed class SubscribePacket : MqttPacketWithId
{
    private readonly IReadOnlyList<(Utf8String Filter, byte QoS)> filters;

    public SubscribePacket(ushort id, IReadOnlyList<(Utf8String Filter, byte QoS)> filters) : base(id)
    {
        Verify.ThrowIfNullOrEmpty(filters);

        this.filters = filters;
    }

    public IReadOnlyList<(Utf8String Filter, byte QoS)> Filters => filters;

    protected override byte Header => SubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out SubscribePacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if (SPE.TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == SubscribeMask
            && TryReadPayload(span.Slice(offset, length), out var id, out var filters))
        {
            consumed = offset + length;
            packet = new(id, filters.Select(p => ((Utf8String)p.Item1, p.Item2)).ToArray());
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if (SRE.TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && header == SubscribeMask
            && TryReadPayload(ref reader, length, out id, out filters))
        {
            consumed = (int)(remaining - reader.Remaining);
            packet = new(id, filters.Select(p => ((Utf8String)p.Item1, p.Item2)).ToArray());
            return true;
        }

        reader.Rewind(remaining - reader.Remaining);
        packet = null;
        consumed = 0;
        return false;
    }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<(byte[], byte)> filters)
    {
        var span = sequence.FirstSpan;
        if (span.Length >= length)
        {
            return TryReadPayload(span[..length], out id, out filters);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out id, out filters);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out ushort id, out IReadOnlyList<(byte[], byte)> filters)
    {
        id = 0;
        filters = null;

        var remaining = reader.Remaining;

        if (!reader.TryReadBigEndian(out short local))
        {
            return false;
        }

        var list = new List<(byte[], byte)>();

        while (remaining - reader.Remaining < length && SRE.TryReadMqttString(ref reader, out var filter))
        {
            if (!reader.TryRead(out var qos)) return false;
            list.Add((filter, qos));
        }

        id = (ushort)local;
        filters = list;
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, out ushort id, out IReadOnlyList<(byte[], byte)> filters)
    {
        id = BP.ReadUInt16BigEndian(span);
        span = span[2..];

        var list = new List<(byte[], byte)>();
        while (SPE.TryReadMqttString(in span, out var filter, out var len))
        {
            list.Add((filter, span[len]));
            span = span[(len + 1)..];
        }

        filters = list;
        return true;
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
        span = span[1..];
        span = span[SPE.WriteMqttLengthBytes(ref span, remainingLength)..];
        BP.WriteUInt16BigEndian(span, Id);
        span = span[2..];

        for (var i = 0; i < filters.Count; i++)
        {
            var (filter, qos) = filters[i];
            span = span[SPE.WriteMqttString(ref span, filter.Span)..];
            span[0] = qos;
            span = span[1..];
        }
    }

    #endregion
}