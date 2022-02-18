using System.Buffers;
using System.Net.Mqtt.Extensions;
using static System.String;
using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Packets;

public sealed class PublishPacket : MqttPacket
{
    public PublishPacket(ushort id, byte qoSLevel, string topic,
        ReadOnlyMemory<byte> payload = default, bool retain = false, bool duplicate = false)
    {
        if (id == 0 && qoSLevel != 0) throw new ArgumentException(MissingPacketId, nameof(id));
        if (IsNullOrEmpty(topic)) throw new ArgumentException(NotEmptyStringExpected, nameof(topic));

        Id = id;
        QoSLevel = qoSLevel;
        Topic = topic;
        Payload = payload;
        Retain = retain;
        Duplicate = duplicate;
    }

    public byte QoSLevel { get; }
    public bool Retain { get; }
    public bool Duplicate { get; }
    public string Topic { get; }
    public ushort Id { get; }
    public ReadOnlyMemory<byte> Payload { get; }

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out PublishPacket packet, out int consumed)
    {
        var span = sequence.FirstSpan;
        if (TryReadMqttHeader(in span, out var header, out var length, out var offset)
            && offset + length <= span.Length
            && (header & PublishMask) == PublishMask
            && TryReadPayload(span.Slice(offset, length), header, out var id, out var topic, out var payload))
        {
            packet = new PublishPacket(id,
                (byte)((header >> 1) & QoSMask),
                topic, payload,
                (header & PacketFlags.Retain) == PacketFlags.Retain,
                (header & PacketFlags.Duplicate) == PacketFlags.Duplicate);
            consumed = offset + length;
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if (TryReadMqttHeader(ref reader, out header, out length)
            && length <= reader.Remaining
            && (header & PublishMask) == PublishMask
            && TryReadPayload(ref reader, header, length, out id, out topic, out payload))
        {
            packet = new PublishPacket(id,
                (byte)((header >> 1) & QoSMask),
                topic, payload,
                (header & PacketFlags.Retain) == PacketFlags.Retain,
                (header & PacketFlags.Duplicate) == PacketFlags.Duplicate);
            consumed = (int)(remaining - reader.Remaining);
            return true;
        }

        reader.Advance(remaining - reader.Remaining);

        packet = null;
        consumed = 0;
        return false;
    }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, byte header, int length,
        out ushort id, out string topic, out ReadOnlyMemory<byte> payload)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            return TryReadPayload(span[..length], header, out id, out topic, out payload);
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, header, length, out id, out topic, out payload);
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> span, byte header,
        out ushort id, out string topic, out ReadOnlyMemory<byte> payload)
    {
        id = 0;

        var qosLevel = (byte)((header >> 1) & QoSMask);

        var packetIdLength = qosLevel != 0 ? 2 : 0;

        var topicLength = ReadUInt16BigEndian(span);

        if (span.Length < topicLength + 2 + packetIdLength)
        {
            id = default;
            topic = default;
            payload = default;
            return false;
        }

        topic = UTF8.GetString(span.Slice(2, topicLength));

        span = span[(2 + topicLength)..];

        if (packetIdLength > 0)
        {
            id = ReadUInt16BigEndian(span);
            span = span[2..];
        }

        payload = span.ToArray();

        return true;
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, byte header, int length,
        out ushort id, out string topic, out ReadOnlyMemory<byte> payload)
    {
        var remaining = reader.Remaining;

        var qosLevel = (byte)((header >> 1) & QoSMask);

        short value = 0;

        if (!TryReadMqttString(ref reader, out topic) || qosLevel > 0 && !reader.TryReadBigEndian(out value))
        {
            reader.Rewind(remaining - reader.Remaining);
            id = 0;
            topic = null;
            payload = default;
            return false;
        }

        var buffer = new byte[length - (remaining - reader.Remaining)];
        reader.TryCopyTo(buffer);

        id = (ushort)value;
        payload = new ReadOnlyMemory<byte>(buffer);
        return true;
    }

    public void Deconstruct(out string topic, out ReadOnlyMemory<byte> payload, out byte qos, out bool retain)
    {
        topic = Topic;
        payload = Payload;
        qos = QoSLevel;
        retain = Retain;
    }

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = (QoSLevel != 0 ? 4 : 2) + UTF8.GetByteCount(Topic) + Payload.Length;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public static int GetSize(byte flags, string topic, ReadOnlyMemory<byte> payload, out int remainingLength)
    {
        remainingLength = (((flags >> 1) & QoSMask) != 0 ? 4 : 2) + UTF8.GetByteCount(topic) + payload.Length;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        var flags = (byte)(PublishMask | (QoSLevel << 1));
        if (Retain) flags |= PacketFlags.Retain;
        if (Duplicate) flags |= PacketFlags.Duplicate;
        span[0] = flags;
        span = span[1..];
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        span = span[WriteMqttString(ref span, Topic)..];

        if (QoSLevel != 0)
        {
            WriteUInt16BigEndian(span, Id);
            span = span[2..];
        }

        Payload.Span.CopyTo(span);
    }

    public static void Write(Span<byte> span, int remainingLength, byte flags, ushort id, string topic, ReadOnlySpan<byte> payload)
    {
        span[0] = (byte)(PublishMask | flags);
        span = span[1..];
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        span = span[WriteMqttString(ref span, topic)..];

        if (((flags >> 1) & QoSMask) != 0)
        {
            WriteUInt16BigEndian(span, id);
            span = span[2..];
        }

        payload.CopyTo(span);
    }

    #endregion
}