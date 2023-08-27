using static System.Net.Mqtt.PacketFlags;
using SequenceReaderExtensions = System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets.V3;

public sealed class PublishPacket : IMqttPacket
{
    public PublishPacket(ushort id, byte qoSLevel, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload = default,
        bool retain = false, bool duplicate = false)
    {
        if (id == 0 && qoSLevel != 0) ThrowHelpers.ThrowInvalidPacketId(id);
        Verify.ThrowIfEmpty(topic);

        Id = id;
        QoSLevel = qoSLevel;
        Topic = topic;
        Payload = payload;
        Retain = retain;
        Duplicate = duplicate;
    }

    public ushort Id { get; }
    public byte QoSLevel { get; }
    public bool Retain { get; }
    public bool Duplicate { get; }
    public ReadOnlyMemory<byte> Topic { get; }
    public ReadOnlyMemory<byte> Payload { get; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, bool readPacketId, int length,
        out ushort id, out byte[] topic, out byte[] payload)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            id = 0;
            span = span.Slice(0, length);
            var packetIdLength = readPacketId ? 2 : 0;
            var topicLength = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            if (span.Length < topicLength + packetIdLength)
                goto ret_false;

            topic = span.Slice(0, topicLength).ToArray();
            span = span.Slice(topicLength);

            if (packetIdLength > 0)
            {
                id = BinaryPrimitives.ReadUInt16BigEndian(span);
                span = span.Slice(2);
            }

            payload = span.ToArray();
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence);
            short value = 0;

            if (!SequenceReaderExtensions.TryReadMqttString(ref reader, out topic) || readPacketId && !reader.TryReadBigEndian(out value))
                goto ret_false;

            payload = new byte[length - reader.Consumed];
            reader.TryCopyTo(payload);
            id = (ushort)value;
            return true;
        }

    ret_false:
        id = 0;
        topic = null;
        payload = null;
        return false;
    }

    #region Implementation of IMqttPacket

    public static int GetSize(byte flags, int topicLength, int payloadLength, out int remainingLength)
    {
        remainingLength = ((flags >> 1 & QoSMask) != 0 ? 4 : 2) + topicLength + payloadLength;
        return 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
    }

    public static void Write(Span<byte> span, int remainingLength, byte flags, ushort id, ReadOnlySpan<byte> topic, ReadOnlySpan<byte> payload)
    {
        span[0] = (byte)(PublishMask | flags);
        span = span.Slice(1);
        SpanExtensions.WriteMqttVarByteInteger(ref span, remainingLength);
        SpanExtensions.WriteMqttString(ref span, topic);

        if ((flags >> 1 & QoSMask) != 0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, id);
            span = span.Slice(2);
        }

        payload.CopyTo(span);
    }

    public int Write([NotNull] IBufferWriter<byte> writer, out Span<byte> buffer)
    {
        var remainingLength = (QoSLevel != 0 ? 4 : 2) + Topic.Length + Payload.Length;
        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
        var flags = (byte)(QoSLevel << 1);
        if (Retain) flags |= PacketFlags.Retain;
        if (Duplicate) flags |= PacketFlags.Duplicate;
        var span = buffer = writer.GetSpan(size);
        Write(span, remainingLength, flags, Id, Topic.Span, Payload.Span);
        writer.Advance(size);
        return size;
    }

    #endregion
}