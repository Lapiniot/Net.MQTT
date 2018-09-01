namespace System.Net.Mqtt.Messages
{
    public sealed class PubCompMessage : MqttPubMessageBase
    {
        public PubCompMessage(ushort packetId) : base(packetId)
        {
        }

        protected override PacketType PacketType => PacketType.PubComp;
    }
}