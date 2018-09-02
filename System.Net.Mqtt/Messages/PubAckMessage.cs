namespace System.Net.Mqtt.Messages
{
    public sealed class PubAckMessage : MqttMessageWithId
    {
        public PubAckMessage(ushort packetId) : base(packetId)
        {
        }

        protected override PacketType PacketType
        {
            get { return PacketType.PubAck; }
        }
    }
}