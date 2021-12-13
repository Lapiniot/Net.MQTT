namespace System.Net.Mqtt.Packets;

public class UnsubAckPacket : MqttPacketWithId
{
    public UnsubAckPacket(ushort id) : base(id) { }

    protected override byte Header { get; } = PacketFlags.UnsubAckMask;
}