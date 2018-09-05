namespace System.Net.Mqtt.Packets
{
    public sealed class PubRecPacket : MqttPacketWithId
    {
        public PubRecPacket(ushort id) : base(id)
        {
        }

        protected override PacketType PacketType
        {
            get { return PacketType.PubRec; }
        }
    }
}