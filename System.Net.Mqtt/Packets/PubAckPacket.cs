namespace System.Net.Mqtt.Packets;

public sealed class PubAckPacket : MqttPacketWithId
{
    public PubAckPacket(ushort id) : base(id) { }

    protected override byte Header { get; } = 0b0100_0000;
}