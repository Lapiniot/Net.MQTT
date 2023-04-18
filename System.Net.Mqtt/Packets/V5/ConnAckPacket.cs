namespace System.Net.Mqtt.Packets.V5;

public sealed class ConnAckPacket : MqttPacket
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

    private readonly byte sessionPresentFlag;

    public ConnAckPacket(byte statusCode, bool sessionPresent = false)
    {
        StatusCode = statusCode;
        sessionPresentFlag = (byte)(sessionPresent ? 0x1 : 0x0);
    }

    public byte StatusCode { get; }
    public bool SessionPresent => sessionPresentFlag == 0x1;
    public uint SessionExpiryInterval { get; init; }
    public ushort ReceiveMaximum { get; init; }
    public QoSLevel MaximumQoS { get; init; } = QoSLevel.QoS2;
    public bool RetainAvailable { get; init; } = true;
    public bool WildcardSubscriptionAvailable { get; init; } = true;
    public bool SubscriptionIdentifiersAvailable { get; init; } = true;
    public bool SharedSubscriptionAvailable { get; init; } = true;
    public uint MaximumPacketSize { get; init; }
    public ushort ServerKeepAlive { get; init; }
    public ReadOnlyMemory<byte> AssignedClientId { get; init; }
    public ushort TopicAliasMaximum { get; init; }
    public ReadOnlyMemory<byte> ReasonString { get; init; }
    public ReadOnlyMemory<byte> ResponseInfo { get; init; }
    public ReadOnlyMemory<byte> ServerReference { get; init; }
    public ReadOnlyMemory<byte> AuthMethod { get; init; }
    public ReadOnlyMemory<byte> AuthData { get; init; }
    public IReadOnlyCollection<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>> Properties { get; init; }

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

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        var propsSize = (SessionExpiryInterval != 0 ? 5 : 0) + (ReceiveMaximum != 0 ? 3 : 0) +
            (MaximumQoS != QoSLevel.QoS2 ? 2 : 0) + (!RetainAvailable ? 2 : 0) + (MaximumPacketSize != 0 ? 5 : 0) +
            (!AssignedClientId.IsEmpty ? AssignedClientId.Length + 3 : 0) + (TopicAliasMaximum != 0 ? 3 : 0) +
            (!ReasonString.IsEmpty ? ReasonString.Length + 3 : 0) + (Properties?.Count > 0 ? GetPropertiesSize(Properties) : 0) +
            (!WildcardSubscriptionAvailable ? 2 : 0) + (!SubscriptionIdentifiersAvailable ? 2 : 0) +
            (!SharedSubscriptionAvailable ? 2 : 0) + (!ResponseInfo.IsEmpty ? 3 + ResponseInfo.Length : 0) +
            (!ServerReference.IsEmpty ? 3 + ServerReference.Length : 0) + (!AuthMethod.IsEmpty ? 3 + AuthMethod.Length : 0) +
            (!AuthData.IsEmpty ? 3 + AuthData.Length : 0);
        remainingLength = 2 + MqttExtensions.GetLengthByteCount(propsSize) + propsSize;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    private static int GetPropertiesSize(IReadOnlyCollection<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>> properties)
    {
        var total = 0;
        foreach (var (key, value) in properties)
        {
            total += 5 + key.Length + value.Length;
        }

        return total;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        // Writes are ordered in this way to eliminated extra bounds checks
        span[4] = 0;
        span[3] = StatusCode;
        span[2] = sessionPresentFlag;
        span[1] = 3;
        span[0] = PacketFlags.ConnAckMask;
    }

    #endregion
}