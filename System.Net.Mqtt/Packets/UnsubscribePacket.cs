using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Properties;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Net.Mqtt.Packets;

public class UnsubscribePacket : MqttPacketWithId
{
    public UnsubscribePacket(ushort id, params string[] topics) : base(id)
    {
        Topics = topics ?? throw new ArgumentNullException(nameof(topics));
        if(topics.Length == 0) throw new ArgumentException(Strings.NotEmptyCollectionExpected);
    }

    public IEnumerable<string> Topics { get; }

    protected override byte Header => 0b10100010;

    public static bool TryRead(ReadOnlySequence<byte> sequence, out UnsubscribePacket packet, out int consumed)
    {
        if(sequence.IsSingleSegment) return TryRead(sequence.First.Span, out packet, out consumed);

        var sr = new SequenceReader<byte>(sequence);
        return TryRead(ref sr, out packet, out consumed);
    }

    public static bool TryRead(ref SequenceReader<byte> reader, out UnsubscribePacket packet, out int consumed)
    {
        if(reader.Sequence.IsSingleSegment) return TryRead(reader.UnreadSpan, out packet, out consumed);

        consumed = 0;
        packet = null;
        var remaining = reader.Remaining;

        if(!reader.TryReadMqttHeader(out var header, out var size) || size > reader.Remaining ||
           header != 0b10100010 || !TryReadPayload(ref reader, size, out packet))
        {
            reader.Rewind(remaining - reader.Remaining);
            return false;
        }

        consumed = (int)(remaining - reader.Remaining);
        return true;
    }

    public static bool TryRead(ReadOnlySpan<byte> span, out UnsubscribePacket packet, out int consumed)
    {
        consumed = 0;
        packet = null;

        if(!span.TryReadMqttHeader(out var header, out var size, out var offset) || offset + size > span.Length ||
           header != 0b10100010 || !TryReadPayload(span[offset..], size, out packet))
        {
            return false;
        }

        consumed = offset + size;
        return true;
    }

    public static bool TryReadPayload(ReadOnlySequence<byte> sequence, int size, out UnsubscribePacket packet)
    {
        packet = null;
        if(sequence.Length < size) return false;
        if(sequence.IsSingleSegment) return TryReadPayload(sequence.First.Span, size, out packet);

        var sr = new SequenceReader<byte>(sequence);
        return TryReadPayload(ref sr, size, out packet);
    }

    public static bool TryReadPayload(ref SequenceReader<byte> reader, int size, out UnsubscribePacket packet)
    {
        packet = null;
        if(reader.Remaining < size) return false;
        if(reader.Sequence.IsSingleSegment) return TryReadPayload(reader.UnreadSpan, size, out packet);

        var remaining = reader.Remaining;

        if(!reader.TryReadBigEndian(out ushort id)) return false;

        var list = new List<string>();

        while(remaining - reader.Remaining < size && reader.TryReadMqttString(out var topic))
        {
            list.Add(topic);
        }

        var consumed = remaining - reader.Remaining;
        if(consumed < size)
        {
            reader.Rewind(consumed);
            return false;
        }

        packet = new UnsubscribePacket(id, list.ToArray());
        return true;
    }

    public static bool TryReadPayload(ReadOnlySpan<byte> span, int size, out UnsubscribePacket packet)
    {
        packet = null;
        if(span.Length < size) return false;
        if(span.Length > size) span = span.Slice(0, size);

        var id = ReadUInt16BigEndian(span);
        span = span[2..];

        var list = new List<string>();
        while(span.TryReadMqttString(out var topic, out var len))
        {
            list.Add(topic);
            span = span[len..];
        }

        if(span.Length > 0) return false;

        packet = new UnsubscribePacket(id, list.ToArray());

        return true;
    }

    #region Overrides of MqttPacketWithId

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = 0b10100010;
        span = span[1..];
        span = span[SpanExtensions.WriteMqttLengthBytes(ref span, remainingLength)..];
        WriteUInt16BigEndian(span, Id);
        span = span[2..];
        foreach(var topic in Topics) span = span[SpanExtensions.WriteMqttString(ref span, topic)..];
    }

    #endregion

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength)
    {
        remainingLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t) + 2) + 2;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    #endregion
}