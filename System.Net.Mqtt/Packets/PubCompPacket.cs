namespace System.Net.Mqtt.Packets
{
    public sealed class PubCompPacket : MqttPacketWithId
    {
        public PubCompPacket(ushort id) : base(id)
        {
        }

        protected override PacketType PacketType
        {
            get { return PacketType.PubComp; }
        }
    }
}