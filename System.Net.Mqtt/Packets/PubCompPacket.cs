using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubCompPacket : MqttPacketWithId
    {
        public PubCompPacket(ushort id) : base(id) {}

        protected override byte Header { get; } = (byte)PubComp;
    }
}