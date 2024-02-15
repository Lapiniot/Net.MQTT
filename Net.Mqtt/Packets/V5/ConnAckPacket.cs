using static Net.Mqtt.Extensions.SpanExtensions;
using static System.Buffers.Binary.BinaryPrimitives;
using static Net.Mqtt.Extensions.SequenceReaderExtensions;
using static Net.Mqtt.MqttHelpers;

namespace Net.Mqtt.Packets.V5;

public sealed class ConnAckPacket(byte statusCode, bool sessionPresent = false) : IMqttPacket5
{
    #region CONNACK reason codes
    public const byte Accepted = 0x00;
    public const byte UnspecifiedError = 0x80;
    public const byte MalformedPacket = 0x81;
    public const byte ProtocolError = 0x82;
    public const byte ImplementationSpecificError = 0x83;
    public const byte UnsupportedProtocolVersion = 0x84;
    public const byte ClientIdentifierNotValid = 0x85;
    public const byte BadUserNameOrPassword = 0x86;
    public const byte NotAuthorized = 0x87;
    public const byte ServerUnavailable = 0x88;
    public const byte ServerBusy = 0x89;
    public const byte Banned = 0x8A;
    public const byte BadAuthenticationMethod = 0x8C;
    public const byte TopicNameInvalid = 0x90;
    public const byte PacketTooLarge = 0x95;
    public const byte QuotaExceeded = 0x97;
    public const byte PayloadFormatInvalid = 0x97;
    public const byte RetainNotSupported = 0x9A;
    public const byte QoSNotSupported = 0x9B;
    public const byte UseAnotherServer = 0x9C;
    public const byte ServerMoved = 0x9D;
    public const byte ConnectionRateExceeded = 0x9F;
    #endregion

    private readonly byte sessionPresentFlag = (byte)(sessionPresent ? 0x1 : 0x0);

    public byte StatusCode { get; } = statusCode;
    public bool SessionPresent => sessionPresentFlag == 0x1;
    public uint? SessionExpiryInterval { get; init; }
    public ushort ReceiveMaximum { get; init; } = ushort.MaxValue;
    public QoSLevel MaximumQoS { get; init; } = QoSLevel.QoS2;
    public bool RetainAvailable { get; init; } = true;
    public bool WildcardSubscriptionAvailable { get; init; } = true;
    public bool SubscriptionIdentifiersAvailable { get; init; } = true;
    public bool SharedSubscriptionAvailable { get; init; } = true;
    public uint? MaximumPacketSize { get; init; }
    public ushort? ServerKeepAlive { get; init; }
    public ReadOnlyMemory<byte> AssignedClientId { get; init; }
    public ushort TopicAliasMaximum { get; init; }
    public ReadOnlyMemory<byte> ReasonString { get; init; }
    public ReadOnlyMemory<byte> ResponseInfo { get; init; }
    public ReadOnlyMemory<byte> ServerReference { get; init; }
    public ReadOnlyMemory<byte> AuthMethod { get; init; }
    public ReadOnlyMemory<byte> AuthData { get; init; }
    public IReadOnlyList<UserProperty> UserProperties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, out ConnAckPacket packet)
    {
        packet = null;

        if (sequence.IsSingleSegment)
        {
            // Hot path
            if (sequence.FirstSpan is not { Length: > 2 } span) return false;

            var statusCode = span[1];
            var sessionPresent = (span[0] & 0x01) != 0;

            span = span.Slice(2);

            if (TryReadMqttVarByteInteger(span, out var propLength, out var consumed) && span.Length >= propLength + consumed)
            {
                if (propLength is 0)
                {
                    packet = new(statusCode, sessionPresent);
                    return true;
                }

                span = span.Slice(consumed, propLength);
                uint? sessionExpiryInterval = null, maximumPacketSize = null;
                byte? maximumQoS = null; ushort? receiveMaximum = null, topicAliasMaximum = null, serverKeepAlive = null;
                byte[] reasonString = null, serverReference = null, authMethod = null, authData = null,
                    assignedClientId = null, responseInformation = null;
                bool? retainAvailable = null, sharedSubscriptionAvailable = null,
                    subscriptionIdentifiersAvailable = null, wildcardSubscriptionAvailable = null;
                List<UserProperty> props = null;

                while (span.Length > 0)
                {
                    switch (span[0])
                    {
                        case 0x11:
                            if (sessionExpiryInterval is { } || !TryReadUInt32BigEndian(span.Slice(1), out var v32))
                                return false;
                            sessionExpiryInterval = v32;
                            span = span.Slice(5);
                            break;
                        case 0x12:
                            if (assignedClientId is { } || !TryReadMqttString(span.Slice(1), out assignedClientId, out var count))
                                return false;
                            span = span.Slice(count + 1);
                            break;
                        case 0x13:
                            if (serverKeepAlive is { } || !TryReadUInt16BigEndian(span.Slice(1), out var v16))
                                return false;
                            serverKeepAlive = v16;
                            span = span.Slice(3);
                            break;
                        case 0x15:
                            if (authMethod is { } || !TryReadMqttString(span.Slice(1), out authMethod, out count))
                                return false;
                            span = span.Slice(count + 1);
                            break;
                        case 0x16:
                            if (authData is { } || !TryReadUInt16BigEndian(span.Slice(1), out var len) || span.Length < len + 2)
                                return false;
                            authData = span.Slice(3, len).ToArray();
                            span = span.Slice(len + 3);
                            break;
                        case 0x1a:
                            if (responseInformation is { } || !TryReadMqttString(span.Slice(1), out responseInformation, out count))
                                return false;
                            span = span.Slice(count + 1);
                            break;
                        case 0x1c:
                            if (serverReference is { } || !TryReadMqttString(span.Slice(1), out serverReference, out count))
                                return false;
                            span = span.Slice(count + 1);
                            break;
                        case 0x1f:
                            if (reasonString is { } || !TryReadMqttString(span.Slice(1), out reasonString, out count))
                                return false;
                            span = span.Slice(count + 1);
                            break;
                        case 0x21:
                            if (receiveMaximum is { } || !TryReadUInt16BigEndian(span.Slice(1), out v16) || v16 is 0)
                                return false;
                            receiveMaximum = v16;
                            span = span.Slice(3);
                            break;
                        case 0x22:
                            if (topicAliasMaximum is { } || !TryReadUInt16BigEndian(span.Slice(1), out v16))
                                return false;
                            topicAliasMaximum = v16;
                            span = span.Slice(3);
                            break;
                        case 0x24:
                            if (maximumQoS is { } || span.Length is 1 || span[1] is not (< 2 and var qos))
                                return false;
                            maximumQoS = qos;
                            span = span.Slice(2);
                            break;
                        case 0x25:
                            if (retainAvailable is { } || span.Length is 1 || span[1] is not (< 2 and var rav))
                                return false;
                            retainAvailable = rav is 1;
                            span = span.Slice(2);
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
                        case 0x27:
                            if (maximumPacketSize is { } || !TryReadUInt32BigEndian(span.Slice(1), out v32) || v32 is 0)
                                return false;
                            maximumPacketSize = v32;
                            span = span.Slice(5);
                            break;
                        case 0x28:
                            if (wildcardSubscriptionAvailable is { } || span.Length is 1 || span[1] is not (< 2 and var wsav))
                                return false;
                            wildcardSubscriptionAvailable = wsav is 1;
                            span = span.Slice(2);
                            break;
                        case 0x29:
                            if (subscriptionIdentifiersAvailable is { } || span.Length is 1 || span[1] is not (< 2 and var siav))
                                return false;
                            subscriptionIdentifiersAvailable = siav is 1;
                            span = span.Slice(2);
                            break;
                        case 0x2a:
                            if (sharedSubscriptionAvailable is { } || span.Length is 1 || span[1] is not (< 2 and var ssav))
                                return false;
                            sharedSubscriptionAvailable = ssav is 1;
                            span = span.Slice(2);
                            break;
                        default: return false;
                    }
                }

                packet = new(statusCode, sessionPresent)
                {
                    AssignedClientId = assignedClientId,
                    AuthData = authData,
                    AuthMethod = authMethod,
                    MaximumPacketSize = maximumPacketSize,
                    MaximumQoS = (QoSLevel)maximumQoS.GetValueOrDefault(2),
                    ReasonString = reasonString,
                    ReceiveMaximum = receiveMaximum.GetValueOrDefault(ushort.MaxValue),
                    ResponseInfo = responseInformation,
                    RetainAvailable = retainAvailable.GetValueOrDefault(true),
                    SharedSubscriptionAvailable = sharedSubscriptionAvailable.GetValueOrDefault(true),
                    SubscriptionIdentifiersAvailable = subscriptionIdentifiersAvailable.GetValueOrDefault(true),
                    WildcardSubscriptionAvailable = wildcardSubscriptionAvailable.GetValueOrDefault(true),
                    ServerKeepAlive = serverKeepAlive,
                    ServerReference = serverReference,
                    TopicAliasMaximum = topicAliasMaximum.GetValueOrDefault(0),
                    UserProperties = props?.AsReadOnly(),
                    SessionExpiryInterval = sessionExpiryInterval,
                };

                return true;
            }

            return false;
        }

        // Slow path
        var reader = new SequenceReader<byte>(sequence);
        return TryReadPayload(ref reader, out packet);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, out ConnAckPacket packet)
    {
        packet = null;
        if (!reader.TryReadBigEndian(out short value)) return false;

        var statusCode = (byte)(value & 0xFF);
        var sessionPresent = (value >>> 8 & 0x01) != 0x00;

        if (!TryReadMqttVarByteInteger(ref reader, out var propLength)) return false;

        if (propLength is 0)
        {
            packet = new(statusCode, sessionPresent);
            return true;
        }

        if (!reader.TryReadExact(propLength, out var sequence)) return false;
        reader = new(sequence);

        uint? sessionExpiryInterval = null, maximumPacketSize = null;
        byte? maximumQoS = null; ushort? receiveMaximum = null, topicAliasMaximum = null, serverKeepAlive = null;
        byte[] reasonString = null, serverReference = null, authMethod = null, authData = null,
            assignedClientId = null, responseInformation = null;
        bool? retainAvailable = null, sharedSubscriptionAvailable = null,
            subscriptionIdentifiersAvailable = null, wildcardSubscriptionAvailable = null;
        List<UserProperty> props = null;

        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x11:
                    if (sessionExpiryInterval is { } || !reader.TryReadBigEndian(out int v32))
                        return false;
                    sessionExpiryInterval = (uint)v32;
                    break;
                case 0x12:
                    if (assignedClientId is { } || !TryReadMqttString(ref reader, out assignedClientId))
                        return false;
                    break;
                case 0x13:
                    if (serverKeepAlive is { } || !reader.TryReadBigEndian(out short v16))
                        return false;
                    serverKeepAlive = (ushort)v16;
                    break;
                case 0x15:
                    if (authMethod is { } || !TryReadMqttString(ref reader, out authMethod))
                        return false;
                    break;
                case 0x16:
                    if (authData is { } || !reader.TryReadBigEndian(out short len))
                        return false;
                    authData = new byte[len];
                    if (!reader.TryCopyTo(authData))
                        return false;
                    reader.Advance(len);
                    break;
                case 0x1a:
                    if (responseInformation is { } || !TryReadMqttString(ref reader, out responseInformation))
                        return false;
                    break;
                case 0x1c:
                    if (serverReference is { } || !TryReadMqttString(ref reader, out serverReference))
                        return false;
                    break;
                case 0x1f:
                    if (reasonString is { } || !TryReadMqttString(ref reader, out reasonString))
                        return false;
                    break;
                case 0x21:
                    if (receiveMaximum is { } || !reader.TryReadBigEndian(out v16) || v16 is 0)
                        return false;
                    receiveMaximum = (ushort)v16;
                    break;
                case 0x22:
                    if (topicAliasMaximum is { } || !reader.TryReadBigEndian(out v16))
                        return false;
                    topicAliasMaximum = (ushort)v16;
                    break;
                case 0x24:
                    if (maximumQoS is { } || !reader.TryRead(out var b) || b > 1)
                        return false;
                    maximumQoS = b;
                    break;
                case 0x25:
                    if (retainAvailable is { } || !reader.TryRead(out b) || b > 1)
                        return false;
                    retainAvailable = b is 1;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key))
                        return false;
                    if (!TryReadMqttString(ref reader, out var val))
                        return false;
                    (props ??= []).Add(new(key, val));
                    break;
                case 0x27:
                    if (maximumPacketSize is { } || !reader.TryReadBigEndian(out v32) || v32 is 0)
                        return false;
                    maximumPacketSize = (uint)v32;
                    break;
                case 0x28:
                    if (wildcardSubscriptionAvailable is { } || !reader.TryRead(out b) || b > 1)
                        return false;
                    wildcardSubscriptionAvailable = b is 1;
                    break;
                case 0x29:
                    if (subscriptionIdentifiersAvailable is { } || !reader.TryRead(out b) || b > 1)
                        return false;
                    subscriptionIdentifiersAvailable = b is 1;
                    break;
                case 0x2a:
                    if (sharedSubscriptionAvailable is { } || !reader.TryRead(out b) || b > 1)
                        return false;
                    sharedSubscriptionAvailable = b is 1;
                    break;
                default: return false;
            }
        }

        packet = new(statusCode, sessionPresent)
        {
            AssignedClientId = assignedClientId,
            AuthData = authData,
            AuthMethod = authMethod,
            MaximumPacketSize = maximumPacketSize,
            MaximumQoS = (QoSLevel)maximumQoS.GetValueOrDefault(2),
            ReasonString = reasonString,
            ReceiveMaximum = receiveMaximum.GetValueOrDefault(ushort.MaxValue),
            ResponseInfo = responseInformation,
            RetainAvailable = retainAvailable.GetValueOrDefault(true),
            SharedSubscriptionAvailable = sharedSubscriptionAvailable.GetValueOrDefault(true),
            SubscriptionIdentifiersAvailable = subscriptionIdentifiersAvailable.GetValueOrDefault(true),
            WildcardSubscriptionAvailable = wildcardSubscriptionAvailable.GetValueOrDefault(true),
            ServerKeepAlive = serverKeepAlive,
            ServerReference = serverReference,
            TopicAliasMaximum = topicAliasMaximum.GetValueOrDefault(0),
            UserProperties = props?.AsReadOnly(),
            SessionExpiryInterval = sessionExpiryInterval,
        };

        return true;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes)
    {
        var reasonStringSize = ReasonString.Length is not 0 and var len ? 3 + len : 0;
        var userPropertiesSize = GetUserPropertiesSize(UserProperties);

        var propsSize = (SessionExpiryInterval is { } ? 5 : 0) + (ReceiveMaximum is not ushort.MaxValue ? 3 : 0) +
            (MaximumQoS is not QoSLevel.QoS2 ? 2 : 0) + (!RetainAvailable ? 2 : 0) + (MaximumPacketSize is { } ? 5 : 0) +
            (!AssignedClientId.IsEmpty ? AssignedClientId.Length + 3 : 0) + (TopicAliasMaximum is not 0 ? 3 : 0) +
            reasonStringSize + userPropertiesSize +
            (!WildcardSubscriptionAvailable ? 2 : 0) + (!SubscriptionIdentifiersAvailable ? 2 : 0) +
            (!SharedSubscriptionAvailable ? 2 : 0) + (ServerKeepAlive is { } ? 3 : 0) +
            (!ResponseInfo.IsEmpty ? 3 + ResponseInfo.Length : 0) + (!ServerReference.IsEmpty ? 3 + ServerReference.Length : 0) +
            (!AuthMethod.IsEmpty ? 3 + AuthMethod.Length : 0) + (!AuthData.IsEmpty ? 3 + AuthData.Length : 0);

        var size = ComputeAdjustedSizes(maxAllowedBytes, 2, ref propsSize, ref reasonStringSize, ref userPropertiesSize, out var remainingLength);

        if (size > maxAllowedBytes)
            return 0;

        var span = writer.GetSpan(size);

        span[0] = PacketFlags.ConnAckMask;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        span[1] = StatusCode;
        span[0] = sessionPresentFlag;
        span = span.Slice(2);
        WriteMqttVarByteInteger(ref span, propsSize);

        if (SessionExpiryInterval is { } sessionExpiryInterval)
        {
            span[0] = 0x11;
            WriteUInt32BigEndian(span = span.Slice(1), sessionExpiryInterval);
            span = span.Slice(4);
        }

        if (ReceiveMaximum is not ushort.MaxValue)
        {
            span[0] = 0x21;
            WriteUInt16BigEndian(span = span.Slice(1), ReceiveMaximum);
            span = span.Slice(2);
        }

        if (MaximumQoS is not QoSLevel.QoS2)
        {
            WriteUInt16BigEndian(span, (ushort)(0x2400 | (byte)MaximumQoS));
            span = span.Slice(2);
        }

        if (!RetainAvailable)
        {
            WriteUInt16BigEndian(span, 0x2500);
            span = span.Slice(2);
        }

        if (MaximumPacketSize is { } maxPacketSize)
        {
            span[0] = 0x27;
            WriteUInt32BigEndian(span = span.Slice(1), maxPacketSize);
            span = span.Slice(4);
        }

        if (!AssignedClientId.IsEmpty)
        {
            span[0] = 0x12;
            span = span.Slice(1);
            WriteMqttString(ref span, AssignedClientId.Span);
        }

        if (TopicAliasMaximum is not 0)
        {
            span[0] = 0x22;
            WriteUInt16BigEndian(span = span.Slice(1), TopicAliasMaximum);
            span = span.Slice(2);
        }

        if (reasonStringSize is not 0)
        {
            span[0] = 0x1F;
            span = span.Slice(1);
            WriteMqttString(ref span, ReasonString.Span);
        }

        if (!WildcardSubscriptionAvailable)
        {
            WriteUInt16BigEndian(span, 0x2800);
            span = span.Slice(2);
        }

        if (!SubscriptionIdentifiersAvailable)
        {
            WriteUInt16BigEndian(span, 0x2900);
            span = span.Slice(2);
        }

        if (!SharedSubscriptionAvailable)
        {
            WriteUInt16BigEndian(span, 0x2a00);
            span = span.Slice(2);
        }

        if (ServerKeepAlive is { } serverKeepAlive)
        {
            span[0] = 0x13;
            WriteUInt16BigEndian(span = span.Slice(1), serverKeepAlive);
            span = span.Slice(2);
        }

        if (!ResponseInfo.IsEmpty)
        {
            span[0] = 0x1a;
            span = span.Slice(1);
            WriteMqttString(ref span, ResponseInfo.Span);
        }

        if (!ServerReference.IsEmpty)
        {
            span[0] = 0x1c;
            span = span.Slice(1);
            WriteMqttString(ref span, ServerReference.Span);
        }

        if (!AuthMethod.IsEmpty)
        {
            span[0] = 0x15;
            span = span.Slice(1);
            WriteMqttString(ref span, AuthMethod.Span);
        }

        if (!AuthData.IsEmpty)
        {
            span[0] = 0x16;
            span = span.Slice(1);
            WriteMqttString(ref span, AuthData.Span);
        }

        if (userPropertiesSize is not 0)
        {
            var count = UserProperties.Count;
            for (var i = 0; i < count; i++)
            {
                var (key, value) = UserProperties[i];
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}