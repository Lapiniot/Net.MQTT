namespace System.Net.Mqtt.Messages
{
    public sealed class PubCompMessage : MqttMessageWithId
    {
        public PubCompMessage(ushort packetId) : base(packetId)
        {
        }

        protected override PacketType PacketType => PacketType.PubComp;
    }
}