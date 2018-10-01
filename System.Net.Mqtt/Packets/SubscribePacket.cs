using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net.Mqtt.Packets
{
    public class SubscribePacket : MqttPacketWithId
    {
        public SubscribePacket(ushort id, params (string, QoSLevel)[] topics) : base(id)
        {
            if(id == 0) throw new ArgumentException($"{nameof(id)} cannot have value of 0");

            Topics = new List<(string, QoSLevel)>(topics);
        }

        public List<(string topic, QoSLevel qosLevel)> Topics { get; }

        protected override byte Header { get; } = (byte)PacketType.Subscribe;

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t.topic) + 3);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + MqttHelpers.GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = (byte)(Header | 0b0010);
            m = m.Slice(1);

            m = m.Slice(MqttHelpers.EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var t in Topics)
            {
                m = m.Slice(MqttHelpers.EncodeString(t.topic, m));
                m[0] = (byte)t.qosLevel;
                m = m.Slice(1);
            }

            return buffer;
        }
    }
}