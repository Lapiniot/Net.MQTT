namespace System.Net.Mqtt
{
    public abstract class MqttMessageWithId : MqttMessage
    {
        protected MqttMessageWithId(ushort packetId)
        {
            PacketId = packetId;
        }

        public ushort PacketId { get; }

        protected abstract PacketType PacketType { get; }

        public override Memory<byte> GetBytes()
        {
            return new byte[] {(byte)PacketType, 2, (byte)(PacketId >> 8), (byte)(PacketId & 0x00ff)};
        }
    }
}