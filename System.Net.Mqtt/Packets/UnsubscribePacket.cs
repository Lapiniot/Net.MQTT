using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets;

public class UnsubscribePacket : MqttPacketWithId
{
    private readonly IReadOnlyList<string> filters;

    public UnsubscribePacket(ushort id, IReadOnlyList<string> filters) : base(id)
    {
        Verify.ThrowIfNullOrEmpty(filters);

        this.filters = filters;
    }

    public IReadOnlyList<string> Filters => filters;

    protected override byte Header => UnsubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out UnsubscribePacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if (SPE.TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == UnsubscribeMask
            && TryReadPayload(span.Slice(offset, length), out var id, out var filters))
        {
            consumed = offset + length;
            packet = new(id, filters);
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if (SRE.TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && header == UnsubscribeMask
            && TryReadPayload(ref reader, length, out id, out filters))
        {
            consumed = (int)(remaining - reader.Remaining);
            packet = new(id, filters);
            return true;
        }

        reader.Rewind(remaining - reader.Remaining);
        packet = null;
        consumed = 0;
        return false;
    }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<string> filters)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            return TryReadPayload(span[..length], out id, out filters);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out id, out filters);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out ushort id, out IReadOnlyList<string> filters)
    {
        id = 0;
        filters = null;

        var remaining = reader.Remaining;

        if (!reader.TryReadBigEndian(out short local))
        {
            return false;
        }

        var list = new List<string>();

        while (remaining - reader.Remaining < length && SRE.TryReadMqttString(ref reader, out var filter))
        {
            list.Add(UTF8.GetString(filter.Span));
        }

        id = (ushort)local;
        filters = list;
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, out ushort id, out IReadOnlyList<string> filters)
    {
        id = BP.ReadUInt16BigEndian(span);
        span = span[2..];

        var list = new List<string>();
        while (SPE.TryReadMqttString(in span, out var filter, out var consumed))
        {
            list.Add(UTF8.GetString(filter.Span));
            span = span[consumed..];
        }

        filters = list;
        return true;
    }

    #region Overrides of MqttPacketWithId

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = UnsubscribeMask;
        span = span[1..];
        span = span[SPE.WriteMqttLengthBytes(ref span, remainingLength)..];
        BP.WriteUInt16BigEndian(span, Id);
        span = span[2..];

        for (var i = 0; i < filters.Count; i++)
        {
            span = span[SPE.WriteMqttString(ref span, UTF8.GetBytes(filters[i]))..];
        }
    }

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;

        for (var i = 0; i < filters.Count; i++)
        {
            remainingLength += UTF8.GetByteCount(filters[i]) + 2;
        }

        return 1 + ME.GetLengthByteCount(remainingLength) + remainingLength;
    }

    #endregion
}