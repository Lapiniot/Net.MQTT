namespace System.Net.Mqtt.Packets
{
    public class RawPacket : MqttPacket
    {
        private readonly byte[] bytes;

        public RawPacket(byte[] bytes)
        {
            this.bytes = bytes;
        }

        #region Overrides of MqttPacket

        public override Memory<byte> GetBytes()
        {
            return bytes;
        }

        #endregion
    }
}