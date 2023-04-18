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
        remainingLength = 2;
        return 5;
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