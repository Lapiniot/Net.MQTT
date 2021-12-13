using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Packets;

public class SubscribePacket : MqttPacketWithId
{
    public SubscribePacket(ushort id, IEnumerable<(string, byte)> topics) : base(id)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if(topics.TryGetNonEnumeratedCount(out var count) && count is 0 || topics.Count() is 0)
        {
            throw new ArgumentException(NotEmptyCollectionExpected);
        }

        Topics = topics;
    }

    public IEnumerable<(string topic, byte qosLevel)> Topics { get; }

    protected override byte Header => SubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out SubscribePacket packet, out int consumed)
    {
        ReadOnlySpan<byte> span = sequence.FirstSpan;
        if(TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == SubscribeMask
            && TryReadPayload(span.Slice(offset, length), out packet))
        {
            consumed = offset + length;
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if(TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && header == SubscribeMask
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

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out SubscribePacket packet)
    {
        var span = sequence.FirstSpan;
        if(span.Length >= length)
        {
            return TryReadPayload(span[..length], out packet);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out packet);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out SubscribePacket packet)
    {
        packet = null;

        var remaining = reader.Remaining;

        if(!reader.TryReadBigEndian(out short id))
        {
            return false;
        }

        var topics = new List<(string, byte)>();

        while(remaining - reader.Remaining < length && TryReadMqttString(ref reader, out var topic))
        {
            if(!reader.TryRead(out var qos)) return false;
            topics.Add((topic, qos));
        }

        packet = new SubscribePacket((ushort)id, topics);
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, out SubscribePacket packet)
    {
        packet = null;

        var id = ReadUInt16BigEndian(span);
        span = span[2..];

        var topics = new List<(string, byte)>();
        while(TryReadMqttString(in span, out var topic, out var len))
        {
            topics.Add((topic, span[len]));
            span = span[(len + 1)..];
        }

        packet = new SubscribePacket(id, topics);

        return true;
    }

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength)
    {
        remainingLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t.topic) + 3) + 2;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = SubscribeMask;
        span = span[1..];
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        WriteUInt16BigEndian(span, Id);
        span = span[2..];

        foreach(var (topic, qosLevel) in Topics)
        {
            span = span[WriteMqttString(ref span, topic)..];
            span[0] = qosLevel;
            span = span[1..];
        }
    }

    #endregion
}