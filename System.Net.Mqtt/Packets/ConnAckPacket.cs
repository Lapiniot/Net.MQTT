using System.IO;

namespace System.Net.Mqtt.Packets
{
    public sealed class ConnAckPacket : MqttPacket
    {
        public ConnAckPacket(Span<byte> source)
        {
            if(source.Length < 4 || source[0] != (byte)PacketType.ConnAck || source[1] != 2)
            {
                throw new InvalidDataException("Invalid CONNECT response. Valid CONNACK packet expected.");
            }

            StatusCode = source[3];
            SessionPresent = (source[2] & 0x01) == 0x01;
        }

        public byte StatusCode { get; set; }

        public bool SessionPresent { get; set; }

        #region Overrides of MqttPacket

        public override Memory<byte> GetBytes()
        {
            return new byte[] {(byte)PacketType.ConnAck, 2, (byte)(SessionPresent ? 1 : 0), StatusCode};
        }

        #endregion
    }
}