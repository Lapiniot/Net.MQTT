using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets.V5;

public readonly record struct PublishPacketProperties(byte? PayloadFormat, uint? MessageExpiryInterval, IReadOnlyList<uint> SubscriptionIds,
    ushort? TopicAlias, ReadOnlyMemory<byte> ContentType, ReadOnlyMemory<byte> ResponseTopic, ReadOnlyMemory<byte> CorrelationData,
    IReadOnlyList<Utf8StringPair> UserProperties);

public sealed class PublishPacket : IMqttPacket5
{
    public PublishPacket(ushort id, byte qoSLevel, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload = default,
        bool retain = false, bool duplicate = false)
    {
        if (id == 0 && qoSLevel != 0) ThrowHelpers.ThrowInvalidPacketId(id);

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
    public ReadOnlyMemory<byte> Topic { get; set; }
    public ReadOnlyMemory<byte> Payload { get; }
    public byte PayloadFormat { get; init; }
    public uint? MessageExpiryInterval { get; init; }
    public IReadOnlyList<uint> SubscriptionIds { get; init; }
    public ushort TopicAlias { get; set; }
    public ReadOnlyMemory<byte> ContentType { get; init; }
    public ReadOnlyMemory<byte> ResponseTopic { get; init; }
    public ReadOnlyMemory<byte> CorrelationData { get; init; }
    public IReadOnlyList<Utf8StringPair> UserProperties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, bool readPacketId, int length,
        out ushort id, out byte[] topic, out byte[] payload, out PublishPacketProperties properties)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            id = 0;
            span = span.Slice(0, length);
            var packetIdLength = readPacketId ? 2 : 0;
            var topicLength = ReadUInt16BigEndian(span);
            span = span.Slice(2);

            if (span.Length < topicLength + packetIdLength)
                goto ret_false;

            topic = span.Slice(0, topicLength).ToArray();
            span = span.Slice(topicLength);

            if (packetIdLength > 0)
            {
                id = ReadUInt16BigEndian(span);
                span = span.Slice(2);
            }

            if (!TryReadMqttVarByteInteger(span, out var propLen, out var consumed) ||
                !TryReadProperties(span.Slice(consumed, propLen), out properties))
            {
                goto ret_false;
            }

            span = span.Slice(consumed + propLen);

            payload = span.ToArray();
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence);
            short value = 0;

            if (!TryReadMqttString(ref reader, out topic) || readPacketId && !reader.TryReadBigEndian(out value))
                goto ret_false;

            if (!TryReadMqttVarByteInteger(ref reader, out var propLen) || !TryReadProperties(sequence.Slice(reader.Consumed, propLen), out properties))
            {
                goto ret_false;
            }

            reader.Advance(propLen);

            payload = new byte[length - reader.Consumed];
            reader.TryCopyTo(payload);
            id = (ushort)value;
            return true;
        }

    ret_false:
        id = 0;
        topic = null;
        payload = null;
        properties = default;
        return false;
    }

    private static bool TryReadProperties(ReadOnlySpan<byte> span, out PublishPacketProperties properties)
    {
        properties = default;
        byte? payloadFormat = null;
        uint? messageExpiryInterval = null;
        ushort? topicAlias = null;
        byte[] contentType = null;
        byte[] responseTopic = null;
        byte[] correlationData = null;
        List<uint> subscriptionIds = null;
        List<Utf8StringPair> props = null;

        while (span.Length > 0)
        {
            switch (span[0])
            {
                case 0x01:
                    if (payloadFormat is { } || span.Length < 2)
                        return false;
                    payloadFormat = span[1];
                    span = span.Slice(2);
                    break;
                case 0x02:
                    if (messageExpiryInterval is { } || !TryReadUInt32BigEndian(span.Slice(1), out var v32))
                        return false;
                    messageExpiryInterval = v32;
                    span = span.Slice(5);
                    break;
                case 0x03:
                    if (contentType is not null || !TryReadMqttString(span.Slice(1), out contentType, out var count))
                        return false;
                    span = span.Slice(count + 1);
                    break;
                case 0x08:
                    if (responseTopic is not null || !TryReadMqttString(span.Slice(1), out responseTopic, out count))
                        return false;
                    span = span.Slice(count + 1);
                    break;
                case 0x09:
                    if (correlationData is not null || !TryReadUInt16BigEndian(span.Slice(1), out var len) || span.Length < len + 2)
                        return false;
                    correlationData = span.Slice(3, len).ToArray();
                    span = span.Slice(len + 3);
                    break;
                case 0x0b:
                    if (!TryReadMqttVarByteInteger(span.Slice(1), out var i32, out count))
                        return false;
                    (subscriptionIds ??= new()).Add((uint)i32);
                    span = span.Slice(count + 1);
                    break;
                case 0x23:
                    if (topicAlias is { } || !TryReadUInt16BigEndian(span.Slice(1), out var v16))
                        return false;
                    topicAlias = v16;
                    span = span.Slice(3);
                    break;
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out count))
                        return false;
                    span = span.Slice(count + 1);
                    if (!TryReadMqttString(span, out var value, out count))
                        return false;
                    span = span.Slice(count);
                    (props ??= []).Add(new(key, value));
                    break;
                default: return false;
            }
        }

        properties = new(payloadFormat, messageExpiryInterval, subscriptionIds?.AsReadOnly(),
            topicAlias, contentType, responseTopic, correlationData, props?.AsReadOnly());
        return true;
    }

    private static bool TryReadProperties(in ReadOnlySequence<byte> sequence, out PublishPacketProperties properties)
    {
        properties = default;
        byte? payloadFormat = null;
        uint? messageExpiryInterval = null;
        ushort? topicAlias = null;
        byte[] contentType = null;
        byte[] responseTopic = null;
        byte[] correlationData = null;
        List<uint> subscriptionIds = null;
        List<Utf8StringPair> props = null;
        var reader = new SequenceReader<byte>(sequence);

        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x01:
                    if (payloadFormat is { } || !reader.TryRead(out var b))
                        return false;
                    payloadFormat = b;
                    break;
                case 0x02:
                    if (messageExpiryInterval is { } || !reader.TryReadBigEndian(out int v32))
                        return false;
                    messageExpiryInterval = (uint?)v32;
                    break;
                case 0x03:
                    if (contentType is not null || !TryReadMqttString(ref reader, out contentType))
                        return false;
                    break;
                case 0x08:
                    if (responseTopic is not null || !TryReadMqttString(ref reader, out responseTopic))
                        return false;
                    break;
                case 0x09:
                    if (correlationData is not null || !TryReadMqttString(ref reader, out correlationData))
                        return false;
                    break;
                case 0x0b:
                    if (!TryReadMqttVarByteInteger(ref reader, out v32))
                        return false;
                    (subscriptionIds ??= new()).Add((uint)v32);
                    break;
                case 0x23:
                    if (topicAlias is { } || !reader.TryReadBigEndian(out short v16))
                        return false;
                    topicAlias = (ushort)v16;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out var value))
                        return false;
                    (props ??= []).Add(new(key, value));
                    break;
                default: return false;
            }
        }

        properties = new(payloadFormat, messageExpiryInterval, subscriptionIds?.AsReadOnly(),
            topicAlias, contentType, responseTopic, correlationData, props?.AsReadOnly());
        return true;
    }

    #region Implementation of IMqttPacket5

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes)
    {
        var propsSize = GetPropertiesSize();
        var remainingLength = (QoSLevel != 0 ? 4 : 2) + Topic.Length + Payload.Length + GetVarBytesCount((uint)propsSize) + propsSize;
        var size = 1 + GetVarBytesCount((uint)remainingLength) + remainingLength;

        if (size > maxAllowedBytes)
            return 0;

        var flags = (byte)(QoSLevel << 1);
        if (Retain) flags |= PacketFlags.Retain;
        if (Duplicate) flags |= PacketFlags.Duplicate;
        var span = writer.GetSpan(size);
        span[0] = (byte)(PublishMask | flags);
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        WriteMqttString(ref span, Topic.Span);

        if ((flags >> 1 & QoSMask) != 0)
        {
            WriteUInt16BigEndian(span, Id);
            span = span.Slice(2);
        }

        WriteMqttVarByteInteger(ref span, propsSize);

        if (PayloadFormat is not 0)
        {
            WriteMqttProperty(ref span, 0x01, PayloadFormat);
        }

        if (MessageExpiryInterval is { } expiry)
        {
            WriteMqttProperty(ref span, 0x02, expiry);
        }

        if (!ContentType.IsEmpty)
        {
            WriteMqttProperty(ref span, 0x03, ContentType.Span);
        }

        if (!ResponseTopic.IsEmpty)
        {
            WriteMqttProperty(ref span, 0x08, ResponseTopic.Span);
        }

        if (!CorrelationData.IsEmpty)
        {
            WriteMqttProperty(ref span, 0x09, CorrelationData.Span);
        }

        if (SubscriptionIds is not null)
        {
            var count = SubscriptionIds.Count;
            for (var i = 0; i < count; i++)
            {
                WriteMqttVarByteIntegerProperty(ref span, 0x0b, SubscriptionIds[i]);
            }
        }

        if (TopicAlias is not 0)
        {
            WriteMqttProperty(ref span, 0x23, TopicAlias);
        }

        if (UserProperties is not null)
        {
            var count = UserProperties.Count;
            for (var i = 0; i < count; i++)
            {
                var (key, value) = UserProperties[i];
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        Payload.Span.CopyTo(span);
        writer.Advance(size);
        return size;
    }

    private int GetPropertiesSize() => (PayloadFormat is not 0 ? 2 : 0) +
        (MessageExpiryInterval is { } ? 5 : 0) + (TopicAlias is not 0 ? 3 : 0) +
        (SubscriptionIds is not null ? GetSubscriptionIdPropertiesSize(SubscriptionIds) : 0) +
        (ResponseTopic.Length is not 0 and var rtLen ? 3 + rtLen : 0) +
        (CorrelationData.Length is not 0 and var cdLen ? 3 + cdLen : 0) +
        (ContentType.Length is not 0 and var ctLen ? 3 + ctLen : 0) +
        (UserProperties is not null ? GetUserPropertiesSize(UserProperties) : 0);

    private static int GetSubscriptionIdPropertiesSize(IReadOnlyList<uint> subscriptionIds)
    {
        var count = subscriptionIds.Count;
        var total = count;
        for (var i = 0; i < count; i++)
        {
            total += GetVarBytesCount(subscriptionIds[i]);
        }

        return total;
    }

    #endregion
}