namespace System.Net.Mqtt.Packets;

public class PingPacket : MqttPacket
{
    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 0;
        return 2;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = PacketFlags.PingMask;
        span[1] = 0;
    }

    #endregion
}