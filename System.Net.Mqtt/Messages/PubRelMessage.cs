namespace System.Net.Mqtt.Messages
{
    public sealed class PubRelMessage : MqttMessageWithId
    {
        public PubRelMessage(ushort packetId) : base(packetId)
        {
        }

        protected override PacketType PacketType => PacketType.PubRel;
    }
}