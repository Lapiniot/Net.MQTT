namespace System.Net.Mqtt.Packets
{
    public sealed class PubRelPacket : MqttPacketWithId
    {
        public PubRelPacket(ushort id) : base(id)
        {
        }

        protected override PacketType PacketType
        {
            get { return PacketType.PubRel; }
        }
    }
}