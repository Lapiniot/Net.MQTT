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
    private readonly IReadOnlyList<(string Topic, byte QoS)> topics;

    public SubscribePacket(ushort id, IReadOnlyList<(string, byte)> topics) : base(id)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if(topics.Count is 0)
        {
            throw new ArgumentException(NotEmptyCollectionExpected);
        }

        this.topics = topics;
    }

    public IReadOnlyList<(string Topic, byte QoS)> Topics => topics;

    protected override byte Header => SubscribeMask;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out SubscribePacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if(TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && header == SubscribeMask
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
            && header == SubscribeMask
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

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out IReadOnlyList<(string, byte)> topics)
    {
        var span = sequence.FirstSpan;
        if(span.Length >= length)
        {
            return TryReadPayload(span[..length], out id, out topics);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out id, out topics);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out ushort id, out IReadOnlyList<(string, byte)> topics)
    {
        id = 0;
        topics = null;

        var remaining = reader.Remaining;

        if(!reader.TryReadBigEndian(out short local))
        {
            return false;
        }

        var list = new List<(string, byte)>();

        while(remaining - reader.Remaining < length && TryReadMqttString(ref reader, out var topic))
        {
            if(!reader.TryRead(out var qos)) return false;
            list.Add((topic, qos));
        }

        id = (ushort)local;
        topics = list;
        return true;
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, out ushort id, out IReadOnlyList<(string, byte)> topics)
    {

        id = ReadUInt16BigEndian(span);
        span = span[2..];

        var list = new List<(string, byte)>();
        while(TryReadMqttString(in span, out var topic, out var len))
        {
            list.Add((topic, span[len]));
            span = span[(len + 1)..];
        }

        topics = list;
        return true;
    }

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;
        for(var i = 0; i < topics.Count; i++)
        {
            remainingLength += Encoding.UTF8.GetByteCount(topics[i].Topic) + 3;
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

        for(var i = 0; i < topics.Count; i++)
        {
            var (topic, qos) = topics[i];
            span = span[WriteMqttString(ref span, topic)..];
            span[0] = qos;
            span = span[1..];
        }
    }

    #endregion
}