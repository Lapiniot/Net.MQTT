namespace System.Net.Mqtt.Packets
{
    public sealed class PubRelPacket : MqttPacketWithId
    {
        public PubRelPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PacketType.PubRel | 0b0010;
    }
}