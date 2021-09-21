namespace System.Net.Mqtt.Packets;

public sealed class PubRelPacket : MqttPacketWithId
{
    public PubRelPacket(ushort id) : base(id) { }

    protected override byte Header { get; } = 0b0110_0010;
}