namespace System.Net.Mqtt.Packets
{
    public sealed class ConnAckPacket : MqttPacket
    {
        public ConnAckPacket(byte statusCode, bool sessionPresent)
        {
            StatusCode = statusCode;
            SessionPresent = sessionPresent;
        }

        public byte StatusCode { get; set; }

        public bool SessionPresent { get; set; }

        #region Overrides of MqttPacket

        public override Memory<byte> GetBytes()
        {
            return new byte[] {(byte)PacketType.ConnAck, 2, (byte)(SessionPresent ? 1 : 0), StatusCode};
        }

        #endregion

        public static bool TryParse(Span<byte> source, out ConnAckPacket packet)
        {
            if(source.Length < 4 || source[0] != (byte)PacketType.ConnAck || source[1] != 2)
            {
                packet = null;
                return false;
            }

            packet = new ConnAckPacket(source[3], (source[2] & 0x01) == 0x01);
            return true;
        }
    }
}