using System.Buffers;

namespace System.Net.Mqtt.Packets;

public sealed class ConnAckPacket : MqttPacket
{
    public const byte Accepted = 0x00;
    public const byte ProtocolRejected = 0x01;
    public const byte IdentifierRejected = 0x02;
    public const byte ServerUnavailable = 0x03;
    public const byte CredentialsRejected = 0x04;
    public const byte NotAuthorized = 0x05;

    public ConnAckPacket(byte statusCode, bool sessionPresent = false)
    {
        StatusCode = statusCode;
        SessionPresent = sessionPresent;
    }

    public byte StatusCode { get; set; }

    public bool SessionPresent { get; set; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, out ConnAckPacket packet)
    {
        packet = null;

        var span = sequence.FirstSpan;
        if(span.Length >= 2)
        {
            packet = new ConnAckPacket(span[1], (span[0] & 0x01) == 0x01);
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        if(reader.TryReadBigEndian(out short value))
        {
            packet = new ConnAckPacket((byte)(value & 0xFF), ((value >> 8) & 0x01) == 0x01);
            return true;
        }

        return false;
    }

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;
        return 4;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = PacketFlags.ConnAckMask;
        span[1] = 2;
        span[2] = (byte)(SessionPresent ? 1 : 0);
        span[3] = StatusCode;
    }

    #endregion
}