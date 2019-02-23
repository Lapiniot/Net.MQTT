namespace System.Net.Mqtt.Packets
{
    public sealed class PubRecPacket : MqttPacketWithId
    {
        public PubRecPacket(ushort id) : base(id) {}

        protected override byte Header { get; } = 0b0101_0000;
    }
}