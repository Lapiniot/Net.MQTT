namespace System.Net.Mqtt.Messages
{
    public sealed class PubRecMessage : MqttMessageWithId
    {
        public PubRecMessage(ushort packetId) : base(packetId)
        {
        }

        protected override PacketType PacketType => PacketType.PubRec;
    }
}