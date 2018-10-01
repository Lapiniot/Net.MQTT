namespace System.Net.Mqtt
{
    public abstract class MqttPacketWithId : MqttPacket
    {
        protected MqttPacketWithId(ushort id)
        {
            Id = id;
        }

        public ushort Id { get; }

        protected abstract byte Header { get; }

        public override Memory<byte> GetBytes()
        {
            return new byte[] {Header, 2, (byte)(Id >> 8), (byte)(Id & 0x00ff)};
        }
    }
}