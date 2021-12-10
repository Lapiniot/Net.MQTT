using System.Buffers;
using System.Net.Mqtt.Extensions;

using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets;

public sealed class PublishPacket : MqttPacket
{
    public PublishPacket(ushort id, byte qoSLevel, string topic,
        Memory<byte> payload = default, bool retain = false, bool duplicate = false)
    {
        if(id == 0 && qoSLevel != 0) throw new ArgumentException(MissingPacketId, nameof(id));
        if(IsNullOrEmpty(topic)) throw new ArgumentException(NotEmptyStringExpected, nameof(topic));

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
    public Memory<byte> Payload { get; }

    public static bool TryRead(ReadOnlySpan<byte> span, out PublishPacket packet, out int consumed)
    {
        packet = null;
        consumed = 0;

        if(!span.TryReadMqttHeader(out var header, out var size, out var offset) || offset + size > span.Length || (header & 0b11_0000) != 0b11_0000 ||
           !TryReadPayload(header, size, span[offset..], out packet))
        {
            return false;
        }

        consumed = offset + size;
        return true;
    }

    public static bool TryRead(ref SequenceReader<byte> reader, out PublishPacket packet, out int consumed)
    {
        if(reader.Sequence.IsSingleSegment) return TryRead(reader.UnreadSpan, out packet, out consumed);

        packet = null;
        consumed = 0;

        var remaining = reader.Remaining;

        if(reader.TryReadMqttHeader(out var header, out var size) && size <= reader.Remaining &&
           (header & 0b11_0000) == 0b11_0000 && TryReadPayload(header, size, ref reader, out packet))
        {
            consumed = (int)(remaining - reader.Remaining);
            return true;
        }

        reader.Advance(remaining - reader.Remaining);
        return false;
    }

    public static bool TryRead(ReadOnlySequence<byte> sequence, out PublishPacket packet, out int consumed)
    {
        if(sequence.IsSingleSegment) return TryRead(sequence.First.Span, out packet, out consumed);

        var sr = new SequenceReader<byte>(sequence);
        return TryRead(ref sr, out packet, out consumed);
    }

    public static bool TryReadPayload(byte header, int size, ReadOnlySpan<byte> span, out PublishPacket packet)
    {
        packet = null;
        if(span.Length < size) return false;
        if(span.Length > size) span = span[..size];

        var qosLevel = (byte)((header >> 1) & QoSMask);

        var packetIdLength = qosLevel != 0 ? 2 : 0;

        var topicLength = ReadUInt16BigEndian(span);

        if(span.Length < topicLength + 2 + packetIdLength) return false;

        var topic = UTF8.GetString(span.Slice(2, topicLength));

        span = span[(2 + topicLength)..];

        ushort id = 0;

        if(packetIdLength > 0)
        {
            id = ReadUInt16BigEndian(span);
            span = span[2..];
        }

        packet = new PublishPacket(id, qosLevel, topic, span.ToArray(),
            (header & PacketFlags.Retain) == PacketFlags.Retain,
            (header & PacketFlags.Duplicate) == PacketFlags.Duplicate);

        return true;
    }

    public static bool TryReadPayload(byte header, int size, ref SequenceReader<byte> reader, out PublishPacket packet)
    {
        packet = null;
        if(reader.Remaining < size) return false;
        if(reader.Sequence.IsSingleSegment) return TryReadPayload(header, size, reader.UnreadSpan, out packet);

        var remaining = reader.Remaining;

        var qosLevel = (byte)((header >> 1) & QoSMask);

        short id = 0;

        if(!reader.TryReadMqttString(out var topic) || qosLevel > 0 && !reader.TryReadBigEndian(out id))
        {
            reader.Rewind(remaining - reader.Remaining);
            return false;
        }

        var buffer = new byte[size - (remaining - reader.Remaining)];
        reader.TryCopyTo(buffer);
        packet = new PublishPacket((ushort)id, qosLevel, topic, buffer,
            (header & PacketFlags.Retain) == PacketFlags.Retain,
            (header & PacketFlags.Duplicate) == PacketFlags.Duplicate);

        return true;
    }

    public static bool TryReadPayload(byte header, int size, ReadOnlySequence<byte> sequence, out PublishPacket packet)
    {
        packet = null;
        if(sequence.Length < size) return false;
        if(sequence.IsSingleSegment) return TryReadPayload(header, size, sequence.First.Span, out packet);

        var sr = new SequenceReader<byte>(sequence);

        return TryReadPayload(header, size, ref sr, out packet);
    }

    public void Deconstruct(out string topic, out Memory<byte> payload, out byte qos, out bool retain)
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

    public override void Write(Span<byte> span, int remainingLength)
    {
        var flags = (byte)(0b0011_0000 | (QoSLevel << 1));
        if(Retain) flags |= PacketFlags.Retain;
        if(Duplicate) flags |= PacketFlags.Duplicate;
        span[0] = flags;
        span = span[1..];
        span = span[SpanExtensions.WriteMqttLengthBytes(ref span, remainingLength)..];
        span = span[SpanExtensions.WriteMqttString(ref span, Topic)..];

        if(QoSLevel != 0)
        {
            WriteUInt16BigEndian(span, Id);
            span = span[2..];
        }

        Payload.Span.CopyTo(span);
    }

    #endregion
}