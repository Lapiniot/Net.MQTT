namespace System.Net.Mqtt
{
    public abstract class MqttMessageWithId : MqttMessage
    {
        public ushort PacketId { get; }

        protected MqttMessageWithId(ushort packetId)
        {
            PacketId = packetId;
        }

        protected abstract PacketType PacketType { get; }

        public override Memory<byte> GetBytes()
        {
            return new byte[] { (byte)PacketType, 2, (byte)(PacketId >> 8), (byte)(PacketId & 0x00ff) };
        }
    }
}