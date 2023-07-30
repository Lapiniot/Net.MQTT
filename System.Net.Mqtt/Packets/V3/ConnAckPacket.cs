namespace System.Net.Mqtt.Packets.V3;

public sealed class ConnAckPacket : IMqttPacket
{
    public const byte Accepted = 0x00;
    public const byte ProtocolRejected = 0x01;
    public const byte IdentifierRejected = 0x02;
    public const byte ServerUnavailable = 0x03;
    public const byte CredentialsRejected = 0x04;
    public const byte NotAuthorized = 0x05;
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

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer, out Span<byte> buffer)
    {
        var span = buffer = writer.GetSpan(4);
        // Writes are ordered in this way to eliminated extra bounds checks
        span[3] = StatusCode;
        span[2] = sessionPresentFlag;
        span[1] = 2;
        span[0] = PacketFlags.ConnAckMask;
        writer.Advance(4);
        return 4;
    }

    #endregion
}