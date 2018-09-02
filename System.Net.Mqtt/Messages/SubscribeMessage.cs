using System.Collections.Generic;
using System.Linq;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Messages
{
    public class SubscribeMessage : MqttMessageWithId
    {
        private readonly List<(string topic, QoSLevel qosLevel)> topics;

        public SubscribeMessage(ushort packetId) : base(packetId)
        {
            topics = new List<(string, QoSLevel)>();
        }

        public List<(string topic, QoSLevel qosLevel)> Topics => topics;

        protected override PacketType PacketType => PacketType.Subscribe;

        public override Memory<byte> GetBytes()
        {
            int payloadLength = topics.Sum(t => UTF8.GetByteCount(t.topic) + 3);
            int remainingLength = payloadLength + 2;
            var buffer = new byte[1 + MqttHelpers.GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = (byte)((int)PacketType & 0b0010);
            m = m.Slice(1);

            m = m.Slice(MqttHelpers.EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(PacketId >> 8);
            m[1] = (byte)(PacketId & 0x00ff);
            m = m.Slice(2);

            foreach(var t in topics)
            {
                m = m.Slice(MqttHelpers.EncodeString(t.topic, m));
                m[0] = (byte)t.qosLevel;
                m = m.Slice(1);
            }

            return buffer;
        }
    }
}