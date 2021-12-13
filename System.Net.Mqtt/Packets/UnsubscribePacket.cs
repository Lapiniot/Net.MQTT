using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Properties;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets;

public class UnsubscribePacket : MqttPacketWithId
{
    public UnsubscribePacket(ushort id, IEnumerable<string> topics) : base(id)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if(topics.TryGetNonEnumeratedCount(out var count) && count is 0 || topics.Count() is 0)
        {
            throw new ArgumentException(Strings.NotEmptyCollectionExpected);
        }

        Topics = topics;
    }

    public IEnumerable<string> Topics { get; }

    protected override byte Header => UnsubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out UnsubscribePacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if(TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == UnsubscribeMask
            && TryReadPayload(span.Slice(offset, length), length, out packet))
        {
            consumed = offset + length;
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if(TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && header == UnsubscribeMask
            && TryReadPayload(ref reader, length, out packet))
        {
            consumed = (int)(remaining - reader.Remaining);
            return true;
        }

        reader.Rewind(remaining - reader.Remaining);
        packet = null;
        consumed = 0;
        return false;
    }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out UnsubscribePacket packet)
    {
        var span = sequence.FirstSpan;
        if(length <= span.Length)
        {
            return TryReadPayload(span[..length], length, out packet);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out packet);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out UnsubscribePacket packet)
    {
        packet = null;

        var remaining = reader.Remaining;

        if(!reader.TryReadBigEndian(out short id))
        {
            return false;
        }

        var list = new List<string>();

        while(remaining - reader.Remaining < length && TryReadMqttString(ref reader, out var topic))
        {
            list.Add(topic);
        }

        packet = new UnsubscribePacket((ushort)id, list);
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, int length, out UnsubscribePacket packet)
    {
        packet = null;

        var id = ReadUInt16BigEndian(span);
        span = span[2..];

        var topics = new List<string>();
        while(TryReadMqttString(in span, out var topic, out var consumed))
        {
            topics.Add(topic);
            span = span[consumed..];
        }

        packet = new UnsubscribePacket(id, topics);

        return true;
    }

    #region Overrides of MqttPacketWithId

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = UnsubscribeMask;
        span = span[1..];
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        WriteUInt16BigEndian(span, Id);
        span = span[2..];
        foreach(var topic in Topics)
        {
            span = span[WriteMqttString(ref span, topic)..];
        }
    }

    public override int GetSize(out int remainingLength)
    {
        remainingLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t) + 2) + 2;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    #endregion
}