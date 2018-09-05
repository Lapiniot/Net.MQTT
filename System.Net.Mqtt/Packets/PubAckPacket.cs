namespace System.Net.Mqtt.Packets
{
    public sealed class PubAckPacket : MqttPacketWithId
    {
        public PubAckPacket(ushort id) : base(id)
        {
        }

        protected override PacketType PacketType
        {
            get { return PacketType.PubAck; }
        }
    }
}