using static System.Net.Mqtt.PacketFlags;
using SequenceReaderExtensions = System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets.V3;

public sealed class PublishPacket : IMqttPacket
{
    public PublishPacket(ushort id, QoSLevel qoSLevel, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload = default,
        bool retain = false, bool duplicate = false)
    {
        if (id is 0 ^ qoSLevel is 0) ThrowHelpers.ThrowInvalidPacketId(id);
        ArgumentOutOfRangeException.ThrowIfZero(topic.Length);

        Id = id;
        QoSLevel = qoSLevel;
        Topic = topic;
        Payload = payload;
        Retain = retain;
        Duplicate = duplicate;
    }

    public ushort Id { get; }
    public QoSLevel QoSLevel { get; }
    public bool Retain { get; }
    public bool Duplicate { get; }
    public ReadOnlyMemory<byte> Topic { get; }
    public ReadOnlyMemory<byte> Payload { get; }

    public static bool TryReadPayloadExact(in ReadOnlySequence<byte> sequence, int count, bool readPacketId,
        out ushort id, out ReadOnlyMemory<byte> topic, out ReadOnlyMemory<byte> payload)
    {
        var span = sequence.FirstSpan;
        if (count <= span.Length)
        {
            id = 0;
            span = span.Slice(0, count);
            var packetIdLength = readPacketId ? 2 : 0;
            var topicLength = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            if (span.Length < topicLength + packetIdLength)
                goto ret_false;

            var topicSpan = span.Slice(0, topicLength);
            span = span.Slice(topicLength);

            if (packetIdLength > 0)
            {
                id = BinaryPrimitives.ReadUInt16BigEndian(span);
                span = span.Slice(2);
            }

            var bytes = new byte[topicLength + span.Length];

            topicSpan.CopyTo(bytes);
            span.CopyTo(bytes.AsSpan(topicLength));

            topic = bytes.AsMemory(0, topicLength);
            payload = bytes.AsMemory(topicLength);
            return true;
        }
        else if (count <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence);
            short value = 0;

            if (!SequenceReaderExtensions.TryReadMqttString(ref reader, out var topicBytes) || readPacketId && !reader.TryReadBigEndian(out value))
                goto ret_false;

            var payloadBytes = new byte[count - reader.Consumed];
            reader.TryCopyTo(payloadBytes);

            id = (ushort)value;
            topic = topicBytes;
            payload = payloadBytes;
            return true;
        }

    ret_false:
        id = 0;
        topic = null;
        payload = null;
        return false;
    }

    private static void Write(Span<byte> span, int remainingLength, int flags, ushort id, ReadOnlySpan<byte> topic, ReadOnlySpan<byte> payload)
    {
        span[0] = (byte)(flags | PublishMask);
        span = span.Slice(1);
        SpanExtensions.WriteMqttVarByteInteger(ref span, remainingLength);
        SpanExtensions.WriteMqttString(ref span, topic);

        if ((flags & 0b110) != 0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, id);
            span = span.Slice(2);
        }

        payload.CopyTo(span);
    }

    internal static int Write(PipeWriter output, int flags, ushort id, ReadOnlySpan<byte> topic, ReadOnlySpan<byte> payload)
    {
        var remainingLength = ((flags & 0b110) != 0 ? 4 : 2) + topic.Length + payload.Length;
        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
        Write(output.GetSpan(size), remainingLength, flags, id, topic, payload);
        output.Advance(size);
        return size;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer)
    {
        var remainingLength = (QoSLevel != QoSLevel0 ? 4 : 2) + Topic.Length + Payload.Length;
        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
        var flags = (int)QoSLevel << 1;
        if (Retain) flags |= PacketFlags.Retain;
        if (Duplicate) flags |= PacketFlags.Duplicate;
        var span = writer.GetSpan(size);
        Write(span, remainingLength, flags, Id, Topic.Span, Payload.Span);
        writer.Advance(size);
        return size;
    }

    #endregion
}