using static System.Net.Mqtt.PacketFlags;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using UserProperty = System.Collections.Generic.KeyValuePair<System.ReadOnlyMemory<byte>, System.ReadOnlyMemory<byte>>;

namespace System.Net.Mqtt.Packets.V5;

public sealed class ConnectPacket : MqttPacket, IBinaryReader<ConnectPacket>
{
    public ConnectPacket(ReadOnlyMemory<byte> clientId, byte protocolLevel, ReadOnlyMemory<byte> protocolName,
        ushort keepAlive = 120, bool cleanSession = true, ReadOnlyMemory<byte> userName = default, ReadOnlyMemory<byte> password = default,
        ReadOnlyMemory<byte> willTopic = default, ReadOnlyMemory<byte> willMessage = default, byte willQoS = 0x00, bool willRetain = false)
    {
        ClientId = clientId;
        ProtocolLevel = protocolLevel;
        ProtocolName = protocolName;
        KeepAlive = keepAlive;
        CleanStart = cleanSession;
        UserName = userName;
        Password = password;
        WillTopic = willTopic;
        WillMessage = willMessage;
        WillQoS = willQoS;
        WillRetain = willRetain;
    }

    public ushort KeepAlive { get; }
    public ReadOnlyMemory<byte> UserName { get; }
    public ReadOnlyMemory<byte> Password { get; }
    public ReadOnlyMemory<byte> ClientId { get; }
    public ReadOnlyMemory<byte> WillTopic { get; }
    public ReadOnlyMemory<byte> WillMessage { get; }
    public byte WillQoS { get; }
    public bool WillRetain { get; }
    public uint WillDelayInterval { get; init; }
    public byte WillPayloadFormat { get; init; }
    public uint WillExpiryInterval { get; init; }
    public ReadOnlyMemory<byte> WillContentType { get; init; }
    public ReadOnlyMemory<byte> WillResponseTopic { get; init; }
    public ReadOnlyMemory<byte> WillCorrelationData { get; init; }
    public bool CleanStart { get; }
    public ReadOnlyMemory<byte> ProtocolName { get; }
    public byte ProtocolLevel { get; }
    public uint SessionExpiryInterval { get; init; }
    public ushort ReceiveMaximum { get; init; }
    public ushort TopicAliasMaximum { get; init; }
    public uint MaximumPacketSize { get; init; }
    public bool RequestResponse { get; init; }
    public bool RequestProblem { get; init; }
    public IReadOnlyList<UserProperty> Properties { get; init; }
    public ReadOnlyMemory<byte> AuthenticationMethod { get; init; }
    public ReadOnlyMemory<byte> AuthenticationData { get; init; }

    internal int PayloadSize => 2 + ClientId.Length +
                                (UserName.IsEmpty ? 0 : 2 + UserName.Length) +
                                (Password.IsEmpty ? 0 : 2 + Password.Length) +
                                (WillTopic.IsEmpty ? 0 : 4 + WillTopic.Length + WillMessage.Length);

    internal int HeaderSize => 6 + ProtocolName.Length;

    public IReadOnlyList<UserProperty> WillProperties { get; private set; }

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out ConnectPacket value, out int consumed)
    {
        value = null;
        consumed = 0;

        if (sequence.IsSingleSegment && TryRead(sequence.FirstSpan, out value, out consumed))
            return true;

        var reader = new SequenceReader<byte>(sequence);

        if (TryReadMqttHeader(ref reader, out var header, out var size) && size <= reader.Remaining && header == ConnectMask)
        {
            if (!TryReadMqttString(ref reader, out var protocol) || !reader.TryRead(out var level) ||
                !reader.TryRead(out var connFlags) || !reader.TryReadBigEndian(out short keepAlive))
            {
                return false;
            }

            if (!TryReadMqttVarByteInteger(ref reader, out var propLen) || reader.Remaining < propLen)
                return false;

            if (!TryReadConnectProps(sequence.Slice(reader.Consumed, propLen), out var sessionExpiryInterval, out var authMethod, out var authData,
                out var requestProblem, out var requestResponse, out var receiveMaximum, out var topicAliasMaximum,
                out var maximumPacketSize, out var userProperties))
            {
                return false;
            }

            reader.Advance(propLen);

            if (!TryReadMqttString(ref reader, out var clientId))
                return false;

            byte[] topic = null;
            byte[] willMessage = null;
            uint? willDelayInterval = null;
            byte? payloadFormat = null;
            uint? messageExpiryInterval = null;
            byte[] contentType = null;
            byte[] responseTopic = null;
            byte[] correlationData = null;
            IReadOnlyList<UserProperty> willProperties = null;

            if ((connFlags & WillMask) == WillMask)
            {
                if (!TryReadMqttVarByteInteger(ref reader, out propLen) || reader.Remaining < propLen)
                    return false;

                if (!TryReadWillProps(sequence.Slice(reader.Consumed, propLen), out willDelayInterval, out payloadFormat,
                    out messageExpiryInterval, out contentType, out responseTopic, out correlationData, out willProperties))
                {
                    return false;
                }

                reader.Advance(propLen);

                if (!TryReadMqttString(ref reader, out topic) || !reader.TryReadBigEndian(out short v16))
                    return false;

                var willSize = (ushort)v16;

                if (willSize > 0)
                {
                    willMessage = new byte[willSize];

                    if (!reader.TryCopyTo(willMessage))
                        return false;

                    reader.Advance(willSize);
                }
            }

            byte[] userName = null;
            byte[] password = null;

            if ((connFlags & UserNameMask) == UserNameMask && !TryReadMqttString(ref reader, out userName) ||
                (connFlags & PasswordMask) == PasswordMask && !TryReadMqttString(ref reader, out password))
            {
                return false;
            }

            value = new(clientId, level, protocol, (ushort)keepAlive,
                (connFlags & CleanStartMask) == CleanStartMask, userName, password, topic, willMessage,
                (byte)(connFlags >> 3 & QoSMask), (connFlags & WillRetainMask) == WillRetainMask)
            {
                AuthenticationMethod = authMethod,
                AuthenticationData = authData,
                SessionExpiryInterval = sessionExpiryInterval ?? 0,
                ReceiveMaximum = receiveMaximum ?? 0,
                MaximumPacketSize = maximumPacketSize ?? 0,
                TopicAliasMaximum = topicAliasMaximum ?? 0,
                RequestResponse = requestResponse == 1,
                RequestProblem = requestProblem == 1,
                Properties = userProperties,
                WillDelayInterval = willDelayInterval ?? 0,
                WillExpiryInterval = messageExpiryInterval ?? 0,
                WillPayloadFormat = payloadFormat ?? 0,
                WillContentType = contentType,
                WillResponseTopic = responseTopic,
                WillCorrelationData = correlationData,
                WillProperties = willProperties
            };

            return true;
        }

        return false;
    }

    private static bool TryRead(in ReadOnlySpan<byte> span, out ConnectPacket packet, out int consumed)
    {
        packet = null;
        consumed = 0;

        if (TryReadMqttHeader(in span, out var header, out var size, out var offset) &&
            offset + size <= span.Length && header == ConnectMask)
        {
            var current = span.Slice(offset, size);

            if (!TryReadUInt16BigEndian(current, out var len) || current.Length < len + 8)
                return false;

            var protocol = current.Slice(2, len).ToArray();
            current = current.Slice(len + 2);
            var level = current[0];
            var connFlags = current[1];
            current = current.Slice(2);

            var keepAlive = ReadUInt16BigEndian(current);
            current = current.Slice(2);

            if (!TryReadMqttVarByteInteger(current, out var propLen, out var count) || current.Length < propLen + count ||
                !TryReadConnectProps(current.Slice(count, propLen), out var sessionExpiryInterval, out var authMethod, out var authData,
                out var requestProblem, out var requestResponse, out var receiveMaximum, out var topicAliasMaximum, out var maximumPacketSize,
                out var userProperties))
            {
                return false;
            }

            current = current.Slice(count + propLen);

            len = ReadUInt16BigEndian(current);
            ReadOnlyMemory<byte> clientId = default;

            if (len > 0)
            {
                if (current.Length < len + 2)
                    return false;

                clientId = current.Slice(2, len).ToArray();
            }

            current = current.Slice(len + 2);
            ReadOnlyMemory<byte> willTopic = default;
            byte[] willMessage = default;
            uint? willDelayInterval = null;
            byte? payloadFormat = null;
            uint? messageExpiryInterval = null;
            byte[] contentType = null;
            byte[] responseTopic = null;
            byte[] correlationData = null;
            IReadOnlyList<UserProperty> willProperties = null;

            if ((connFlags & WillMask) == WillMask)
            {
                if (!TryReadMqttVarByteInteger(current, out propLen, out count) || current.Length < propLen + count)
                    return false;

                if (!TryReadWillProps(current.Slice(count, propLen), out willDelayInterval, out payloadFormat, out messageExpiryInterval,
                    out contentType, out responseTopic, out correlationData, out willProperties))
                {
                    return false;
                }

                current = current.Slice(count + propLen);

                if (!TryReadUInt16BigEndian(current, out len) || len == 0 || current.Length < len + 2)
                    return false;

                willTopic = current.Slice(2, len).ToArray();
                current = current.Slice(len + 2);

                if (!TryReadUInt16BigEndian(current, out len) || current.Length < len + 2)
                    return false;

                if (len > 0)
                {
                    willMessage = new byte[len];
                    current.Slice(2, len).CopyTo(willMessage);
                }

                current = current.Slice(len + 2);
            }

            ReadOnlyMemory<byte> userName = default;

            if ((connFlags & UserNameMask) == UserNameMask)
            {
                if (!TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                userName = current.Slice(2, len).ToArray();
                current = current.Slice(len + 2);
            }

            ReadOnlyMemory<byte> password = default;

            if ((connFlags & PasswordMask) == PasswordMask)
            {
                if (!TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                password = current.Slice(2, len).ToArray();
            }

            packet = new(clientId, level, protocol, keepAlive,
                (connFlags & CleanStartMask) == CleanStartMask, userName, password, willTopic, willMessage,
                (byte)(connFlags >> 3 & QoSMask), (connFlags & WillRetainMask) == WillRetainMask)
            {
                AuthenticationMethod = authMethod,
                AuthenticationData = authData,
                SessionExpiryInterval = sessionExpiryInterval ?? 0,
                ReceiveMaximum = receiveMaximum ?? 0,
                MaximumPacketSize = maximumPacketSize ?? 0,
                TopicAliasMaximum = topicAliasMaximum ?? 0,
                RequestResponse = requestResponse == 1,
                RequestProblem = requestProblem == 1,
                Properties = userProperties,
                WillDelayInterval = willDelayInterval ?? 0,
                WillExpiryInterval = messageExpiryInterval ?? 0,
                WillPayloadFormat = payloadFormat ?? 0,
                WillContentType = contentType,
                WillResponseTopic = responseTopic,
                WillCorrelationData = correlationData,
                WillProperties = willProperties
            };

            return true;
        }

        consumed = 0;
        return false;
    }

    private static bool TryReadWillProps(ReadOnlySpan<byte> span, out uint? willDelayInterval, out byte? payloadFormat,
        out uint? messageExpiryInterval, out byte[] contentType, out byte[] responseTopic, out byte[] correlationData,
        out IReadOnlyList<UserProperty> userProperties)
    {
        willDelayInterval = null;
        payloadFormat = null;
        messageExpiryInterval = null;
        contentType = null;
        responseTopic = null;
        correlationData = null;
        userProperties = null;
        List<UserProperty> props = null;

        while (span.Length > 0)
        {
            switch (span[0])
            {
                case 0x01:
                    if (payloadFormat.HasValue || span.Length < 2)
                        return false;
                    payloadFormat = span[1];
                    span = span.Slice(2);
                    break;
                case 0x02:
                    if (messageExpiryInterval.HasValue || !TryReadUInt32BigEndian(span.Slice(1), out var v32))
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
                case 0x18:
                    if (willDelayInterval.HasValue || !TryReadUInt32BigEndian(span.Slice(1), out v32))
                        return false;
                    willDelayInterval = v32;
                    span = span.Slice(5);
                    break;
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out count))
                        return false;
                    span = span.Slice(count + 1);
                    if (!TryReadMqttString(in span, out var value, out count))
                        return false;
                    span = span.Slice(count);
                    (props ??= new List<UserProperty>()).Add(new(key, value));
                    break;
                default: return false;
            }
        }

        userProperties = props;
        return true;
    }

    private static bool TryReadWillProps(ReadOnlySequence<byte> sequence, out uint? willDelayInterval, out byte? payloadFormat,
        out uint? messageExpiryInterval, out byte[] contentType, out byte[] responseTopic, out byte[] correlationData,
        out IReadOnlyList<UserProperty> userProperties)
    {
        willDelayInterval = null;
        payloadFormat = null;
        messageExpiryInterval = null;
        contentType = null;
        responseTopic = null;
        correlationData = null;
        userProperties = null;
        List<UserProperty> props = null;

        var reader = new SequenceReader<byte>(sequence);

        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x01:
                    if (payloadFormat.HasValue || !reader.TryRead(out var b))
                        return false;
                    payloadFormat = b;
                    break;
                case 0x02:
                    if (messageExpiryInterval.HasValue || !reader.TryReadBigEndian(out int v32))
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
                    if (correlationData is not null || !reader.TryReadBigEndian(out short len))
                        return false;
                    correlationData = new byte[len];
                    if (!reader.TryCopyTo(correlationData))
                        return false;
                    reader.Advance(len);
                    break;
                case 0x18:
                    if (willDelayInterval.HasValue || !reader.TryReadBigEndian(out v32))
                        return false;
                    willDelayInterval = (uint?)v32;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out var value))
                        return false;
                    (props ??= new List<UserProperty>()).Add(new(key, value));
                    break;
                default: return false;
            }
        }

        userProperties = props;
        return true;
    }

    private static bool TryReadConnectProps(ReadOnlySpan<byte> span,
        out uint? sessionExpiryInterval, out byte[] authMethod, out byte[] authData,
        out byte? requestProblem, out byte? requestResponse, out ushort? receiveMaximum,
        out ushort? topicAliasMaximum, out uint? maximumPacketSize,
        out IReadOnlyList<UserProperty> userProperties)
    {
        sessionExpiryInterval = null;
        authMethod = null;
        authData = null;
        requestProblem = null;
        requestResponse = null;
        receiveMaximum = null;
        topicAliasMaximum = null;
        maximumPacketSize = null;
        userProperties = null;
        List<UserProperty> props = null;

        while (span.Length > 0)
        {
            switch (span[0])
            {
                case 0x11:
                    if (sessionExpiryInterval.HasValue || !TryReadUInt32BigEndian(span.Slice(1), out var v32))
                        return false;
                    sessionExpiryInterval = v32;
                    span = span.Slice(5);
                    break;
                case 0x15:
                    if (authMethod is not null || !TryReadMqttString(span.Slice(1), out authMethod, out var count))
                        return false;
                    span = span.Slice(count + 1);
                    break;
                case 0x16:
                    if (authData is not null || !TryReadUInt16BigEndian(span.Slice(1), out var len) || span.Length < len + 2)
                        return false;
                    authData = span.Slice(3, len).ToArray();
                    span = span.Slice(len + 3);
                    break;
                case 0x17:
                    if (requestProblem.HasValue || span.Length < 2)
                        return false;
                    requestProblem = span[1];
                    span = span.Slice(2);
                    break;
                case 0x19:
                    if (requestResponse.HasValue || span.Length < 2)
                        return false;
                    requestResponse = span[1];
                    span = span.Slice(2);
                    break;
                case 0x21:
                    if (receiveMaximum.HasValue || !TryReadUInt16BigEndian(span.Slice(1), out var v16))
                        return false;
                    receiveMaximum = v16;
                    span = span.Slice(3);
                    break;
                case 0x22:
                    if (topicAliasMaximum.HasValue || !TryReadUInt16BigEndian(span.Slice(1), out v16))
                        return false;
                    topicAliasMaximum = v16;
                    span = span.Slice(3);
                    break;
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out count))
                        return false;
                    span = span.Slice(count + 1);
                    if (!TryReadMqttString(in span, out var value, out count))
                        return false;
                    span = span.Slice(count);
                    (props ??= new List<UserProperty>()).Add(new(key, value));
                    break;
                case 0x27:
                    if (maximumPacketSize.HasValue || !TryReadUInt32BigEndian(span.Slice(1), out v32))
                        return false;
                    maximumPacketSize = v32;
                    span = span.Slice(5);
                    break;
                default: return false;
            }
        }

        userProperties = props?.AsReadOnly();
        return true;
    }

    private static bool TryReadConnectProps(in ReadOnlySequence<byte> sequence,
        out uint? sessionExpiryInterval, out byte[] authMethod, out byte[] authData,
        out byte? requestProblem, out byte? requestResponse, out ushort? receiveMaximum,
        out ushort? topicAliasMaximum, out uint? maximumPacketSize,
        out IReadOnlyList<UserProperty> userProperties)
    {
        sessionExpiryInterval = null;
        authMethod = null;
        authData = null;
        requestProblem = null;
        requestResponse = null;
        receiveMaximum = null;
        topicAliasMaximum = null;
        maximumPacketSize = null;
        userProperties = null;
        List<UserProperty> props = null;

        var reader = new SequenceReader<byte>(sequence);
        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x11:
                    if (sessionExpiryInterval.HasValue || !reader.TryReadBigEndian(out int v32))
                        return false;
                    sessionExpiryInterval = (uint)v32;
                    break;
                case 0x15:
                    if (authMethod is not null || !TryReadMqttString(ref reader, out var value))
                        return false;
                    authMethod = value;
                    break;
                case 0x16:
                    if (authData is not null || !reader.TryReadBigEndian(out short len))
                        return false;
                    authData = new byte[len];
                    if (!reader.TryCopyTo(authData))
                        return false;
                    reader.Advance(len);
                    break;
                case 0x17:
                    if (requestProblem.HasValue || !reader.TryRead(out var b))
                        return false;
                    requestProblem = b;
                    break;
                case 0x19:
                    if (requestResponse.HasValue || !reader.TryRead(out b))
                        return false;
                    requestResponse = b;
                    break;
                case 0x21:
                    if (receiveMaximum.HasValue || !reader.TryReadBigEndian(out short v16))
                        return false;
                    receiveMaximum = (ushort)v16;
                    break;
                case 0x22:
                    if (topicAliasMaximum.HasValue || !reader.TryReadBigEndian(out v16))
                        return false;
                    topicAliasMaximum = (ushort)v16;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out value))
                        return false;
                    (props ??= new List<UserProperty>()).Add(new(key, value));
                    break;
                case 0x27:
                    if (maximumPacketSize.HasValue || !reader.TryReadBigEndian(out v32))
                        return false;
                    maximumPacketSize = (uint)v32;
                    break;
                default: return false;
            }
        }

        userProperties = props?.AsReadOnly();
        return true;
    }

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = HeaderSize + PayloadSize;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        var hasClientId = !ClientId.IsEmpty;
        var hasUserName = !UserName.IsEmpty;
        var hasPassword = !Password.IsEmpty;
        var hasWillTopic = !WillTopic.IsEmpty;
        var flags = (byte)(WillQoS << 3);
        if (hasUserName) flags |= UserNameMask;
        if (hasPassword) flags |= PasswordMask;
        if (WillRetain) flags |= WillRetainMask;
        if (hasWillTopic) flags |= WillMask;
        if (CleanStart) flags |= CleanSessionMask;

        // Packet flags
        span[0] = ConnectMask;
        span = span.Slice(1);
        // Remaining length bytes
        span = span.Slice(WriteMqttLengthBytes(ref span, remainingLength));
        // Protocol info bytes
        span = span.Slice(WriteMqttString(ref span, ProtocolName.Span));
        span[1] = flags;
        span[0] = ProtocolLevel;
        span = span.Slice(2);
        // KeepAlive bytes
        WriteUInt16BigEndian(span, KeepAlive);
        span = span.Slice(2);

        // Payload bytes
        if (hasClientId)
        {
            span = span.Slice(WriteMqttString(ref span, ClientId.Span));
        }
        else
        {
            span[1] = 0;
            span[0] = 0;
            span = span.Slice(2);
        }

        // Last will
        if (hasWillTopic)
        {
            span = span.Slice(WriteMqttString(ref span, WillTopic.Span));
            var messageSpan = WillMessage.Span;
            var spanLength = messageSpan.Length;
            WriteUInt16BigEndian(span, (ushort)spanLength);
            span = span.Slice(2);
            messageSpan.CopyTo(span);
            span = span.Slice(spanLength);
        }

        // Username
        if (hasUserName)
            span = span.Slice(WriteMqttString(ref span, UserName.Span));

        //Password
        if (hasPassword)
            WriteMqttString(ref span, Password.Span);
    }

    #endregion
}