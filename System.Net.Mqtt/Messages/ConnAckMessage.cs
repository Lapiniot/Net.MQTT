using System.IO;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Messages
{
    public class ConnAckMessage : MqttMessage
    {
        public ConnAckMessage(Span<byte> source)
        {
            if(source.Length < 4 || source[0] != (byte)ConnAck || source[1] != 2)
            {
                throw new InvalidDataException("Invalid CONNECT response. Valid CONNACK packet expected.");
            }

            StatusCode = source[3];
        }

        public byte StatusCode { get; set; }

        #region Overrides of MqttMessage

        public override Memory<byte> GetBytes()
        {
            return new byte[] {(byte)ConnAck, 2, 0, StatusCode};
        }

        #endregion
    }
}