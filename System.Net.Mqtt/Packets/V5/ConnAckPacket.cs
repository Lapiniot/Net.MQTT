using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Packets.V5;

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
    public uint SessionExpiryInterval { get; init; }
    public ushort ReceiveMaximum { get; init; }
    public QoSLevel MaximumQoS { get; init; } = QoSLevel.QoS2;
    public bool RetainAvailable { get; init; } = true;
    public bool WildcardSubscriptionAvailable { get; init; } = true;
    public bool SubscriptionIdentifiersAvailable { get; init; } = true;
    public bool SharedSubscriptionAvailable { get; init; } = true;
    public uint? MaximumPacketSize { get; init; }
    public ushort ServerKeepAlive { get; init; }
    public ReadOnlyMemory<byte> AssignedClientId { get; init; }
    public ushort TopicAliasMaximum { get; init; }
    public ReadOnlyMemory<byte> ReasonString { get; init; }
    public ReadOnlyMemory<byte> ResponseInfo { get; init; }
    public ReadOnlyMemory<byte> ServerReference { get; init; }
    public ReadOnlyMemory<byte> AuthMethod { get; init; }
    public ReadOnlyMemory<byte> AuthData { get; init; }
    public IReadOnlyList<Utf8StringPair> UserProperties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, out ConnAckPacket packet)
    {
        var span = sequence.FirstSpan;
        if (span.Length >= 2)
        {
            packet = new(span[1], (span[0] & 0x01) == 0x01);
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        if (!reader.TryReadBigEndian(out short value))
        {
            packet = null;
            return false;
        }

        packet = new((byte)(value & 0xFF), (value >> 8 & 0x01) == 0x01);
        return true;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes)
    {
        var reasonStringSize = ReasonString.Length is not 0 and var len ? 3 + len : 0;
        var userPropertiesSize = GetUserPropertiesSize(UserProperties);

        var propsSize = (SessionExpiryInterval != 0 ? 5 : 0) + (ReceiveMaximum != 0 ? 3 : 0) +
            (MaximumQoS != QoSLevel.QoS2 ? 2 : 0) + (!RetainAvailable ? 2 : 0) + (MaximumPacketSize is { } ? 5 : 0) +
            (!AssignedClientId.IsEmpty ? AssignedClientId.Length + 3 : 0) + (TopicAliasMaximum != 0 ? 3 : 0) +
            reasonStringSize + userPropertiesSize +
            (!WildcardSubscriptionAvailable ? 2 : 0) + (!SubscriptionIdentifiersAvailable ? 2 : 0) +
            (!SharedSubscriptionAvailable ? 2 : 0) + (ServerKeepAlive != 0 ? 3 : 0) +
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

        if (SessionExpiryInterval != 0)
        {
            span[0] = 0x11;
            WriteUInt32BigEndian(span = span.Slice(1), SessionExpiryInterval);
            span = span.Slice(4);
        }

        if (ReceiveMaximum != 0)
        {
            span[0] = 0x21;
            WriteUInt16BigEndian(span = span.Slice(1), ReceiveMaximum);
            span = span.Slice(2);
        }

        if (MaximumQoS is QoSLevel.QoS0 or QoSLevel.QoS1)
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

        if (TopicAliasMaximum != 0)
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

        if (ServerKeepAlive != 0)
        {
            span[0] = 0x13;
            WriteUInt16BigEndian(span = span.Slice(1), ServerKeepAlive);
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