using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Properties;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets;

public class UnsubscribePacket : MqttPacketWithId
{
    private readonly IReadOnlyList<string> topics;

    public UnsubscribePacket(ushort id, IReadOnlyList<string> topics) : base(id)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if(topics.Count is 0)
        {
            throw new ArgumentException(Strings.NotEmptyCollectionExpected);
        }

        this.topics = topics;
    }

    public IReadOnlyList<string> Topics => topics;

    protected override byte Header => UnsubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out UnsubscribePacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if(TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == UnsubscribeMask
            && TryReadPayload(span.Slice(offset, length), out var id, out var topics))
        {
            consumed = offset + length;
            packet = new(id, topics);
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if(TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && header == UnsubscribeMask
            && TryReadPayload(ref reader, length, out id, out topics))
        {
            consumed = (int)(remaining - reader.Remaining);
            packet = new(id, topics);
            return true;
        }

        reader.Rewind(remaining - reader.Remaining);
        packet = null;
        consumed = 0;
        return false;
    }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<string> topics)
    {
        var span = sequence.FirstSpan;
        if(length <= span.Length)
        {
            return TryReadPayload(span[..length], out id, out topics);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out id, out topics);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out ushort id, out IReadOnlyList<string> topics)
    {
        id = 0;
        topics = null;

        var remaining = reader.Remaining;

        if(!reader.TryReadBigEndian(out short local))
        {
            return false;
        }

        var list = new List<string>();

        while(remaining - reader.Remaining < length && TryReadMqttString(ref reader, out var topic))
        {
            list.Add(topic);
        }

        id = (ushort)local;
        topics = list;
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, out ushort id, out IReadOnlyList<string> topics)
    {
        id = ReadUInt16BigEndian(span);
        span = span[2..];

        var list = new List<string>();
        while(TryReadMqttString(in span, out var topic, out var consumed))
        {
            list.Add(topic);
            span = span[consumed..];
        }

        topics = list;
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

        for(var i = 0; i < topics.Count; i++)
        {
            span = span[WriteMqttString(ref span, topics[i])..];
        }
    }

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;

        for(var i = 0; i < topics.Count; i++)
        {
            remainingLength += UTF8.GetByteCount(topics[i]) + 2;
        }

        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    #endregion
}