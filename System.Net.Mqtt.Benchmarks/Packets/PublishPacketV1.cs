using System.Buffers;
using System.Buffers.Binary;
using System.Net.Mqtt.Extensions;
using static System.Net.Mqtt.PacketFlags;
using SequenceReaderExtensions = System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Benchmarks.Packets;

public sealed class PublishPacketV1 : MqttPacket
{
    public PublishPacketV1(ushort id, byte qoSLevel, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload = default,
        bool retain = false, bool duplicate = false)
    {
        if (id == 0 && qoSLevel != 0) ThrowMissingPacketId(nameof(id));
        Verify.ThrowIfEmpty(topic);

        Id = id;
        QoSLevel = qoSLevel;
        Topic = topic;
        Payload = payload;
        Retain = retain;
        Duplicate = duplicate;
    }

    [DoesNotReturn]
    private static void ThrowMissingPacketId(string paramName) =>
        throw new ArgumentException("Valid packet id must be specified for this QoS level.", paramName);

    public ushort Id { get; }
    public byte QoSLevel { get; }
    public bool Retain { get; }
    public bool Duplicate { get; }
    public ReadOnlyMemory<byte> Topic { get; }
    public ReadOnlyMemory<byte> Payload { get; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, byte header, int length,
        out ushort id, out byte[] topic, out byte[] payload)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            id = 0;
            span = span.Slice(0, length);
            var qos = (byte)((header >> 1) & QoSMask);
            var packetIdLength = qos != 0 ? 2 : 0;
            var topicLength = BinaryPrimitives.ReadUInt16BigEndian(span);

            if (span.Length < 2 + topicLength + packetIdLength)
            {
                goto ret_false;
            }

            topic = span.Slice(2, topicLength).ToArray();
            span = span.Slice(2 + topicLength);

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
            var reader = new SequenceReader<byte>(sequence.Slice(0, length));
            var qos = (byte)((header >> 1) & QoSMask);
            short value = 0;

            if (!SequenceReaderExtensions.TryReadMqttString(ref reader, out topic) || qos > 0 && !reader.TryReadBigEndian(out value))
            {
                goto ret_false;
            }

            payload = new byte[reader.Remaining];
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

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = (QoSLevel != 0 ? 4 : 2) + Topic.Length + Payload.Length;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public static int GetSize(byte flags, int topicLength, int payloadLength, out int remainingLength)
    {
        remainingLength = (((flags >> 1) & QoSMask) != 0 ? 4 : 2) + topicLength + payloadLength;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        var flags = (byte)(QoSLevel << 1);
        if (Retain) flags |= PacketFlags.Retain;
        if (Duplicate) flags |= PacketFlags.Duplicate;

        Write(span, remainingLength, flags, Id, Topic.Span, Payload.Span);
    }

    public static void Write(Span<byte> span, int remainingLength, byte flags, ushort id, ReadOnlySpan<byte> topic, ReadOnlySpan<byte> payload)
    {
        span[0] = (byte)(PublishMask | flags);
        span = span.Slice(1);
        span = span.Slice(SpanExtensions.WriteMqttLengthBytes(ref span, remainingLength));
        span = span.Slice(SpanExtensions.WriteMqttString(ref span, topic));

        if (((flags >> 1) & QoSMask) != 0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, id);
            span = span.Slice(2);
        }

        payload.CopyTo(span);
    }

    #endregion
}