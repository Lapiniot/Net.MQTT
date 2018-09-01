namespace System.Net.Mqtt.Messages
{
    public sealed class PubAckMessage : MqttPubMessageBase
    {
        public PubAckMessage(ushort packetId) : base(packetId)
        {
        }

        protected override PacketType PacketType => PacketType.PubAck;
    }
}