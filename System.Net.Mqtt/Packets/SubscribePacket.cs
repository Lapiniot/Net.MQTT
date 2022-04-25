using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets;

public class SubscribePacket : MqttPacketWithId
{
    private readonly IReadOnlyList<(string Filter, byte QoS)> filters;

    public SubscribePacket(ushort id, IReadOnlyList<(string Filter, byte QoS)> filters) : base(id)
    {
        Verify.ThrowIfNullOrEmpty(filters);

        this.filters = filters;
    }

    public IReadOnlyList<(string Filter, byte QoS)> Filters => filters;

    protected override byte Header => SubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out SubscribePacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if (TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == SubscribeMask
            && TryReadPayload(span.Slice(offset, length), out var id, out var filters))
        {
            consumed = offset + length;
            packet = new(id, filters);
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if (TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && header == SubscribeMask
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

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<(string, byte)> filters)
    {
        var span = sequence.FirstSpan;
        if (span.Length >= length)
        {
            return TryReadPayload(span[..length], out id, out filters);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out id, out filters);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out ushort id, out IReadOnlyList<(string, byte)> filters)
    {
        id = 0;
        filters = null;

        var remaining = reader.Remaining;

        if (!reader.TryReadBigEndian(out short local))
        {
            return false;
        }

        var list = new List<(string, byte)>();

        while (remaining - reader.Remaining < length && TryReadMqttString(ref reader, out var filter))
        {
            if (!reader.TryRead(out var qos)) return false;
            list.Add((Encoding.UTF8.GetString(filter.Span), qos));
        }

        id = (ushort)local;
        filters = list;
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, out ushort id, out IReadOnlyList<(string, byte)> filters)
    {
        id = ReadUInt16BigEndian(span);
        span = span[2..];

        var list = new List<(string, byte)>();
        while (TryReadMqttString(in span, out var filter, out var len))
        {
            list.Add((Encoding.UTF8.GetString(filter.Span), span[len]));
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
            remainingLength += Encoding.UTF8.GetByteCount(filters[i].Filter) + 3;
        }

        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = SubscribeMask;
        span = span[1..];
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        WriteUInt16BigEndian(span, Id);
        span = span[2..];

        for (var i = 0; i < filters.Count; i++)
        {
            var (filter, qos) = filters[i];
            span = span[WriteMqttString(ref span, Encoding.UTF8.GetBytes(filter))..];
            span[0] = qos;
            span = span[1..];
        }
    }

    #endregion
}