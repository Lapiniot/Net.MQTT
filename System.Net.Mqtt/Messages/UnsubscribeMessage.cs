using System.Collections.Generic;
using System.Linq;
using static System.Net.Mqtt.MqttHelpers;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Messages
{
    public class UnsubscribeMessage : MqttMessageWithId
    {
        public UnsubscribeMessage(ushort packetId) : base(packetId)
        {
            if(packetId == 0) throw new ArgumentException($"{nameof(packetId)} cannot have value of 0");

            Topics = new List<string>();
        }

        public List<string> Topics { get; }

        protected override PacketType PacketType
        {
            get { return PacketType.Unsubscribe; }
        }

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => UTF8.GetByteCount(t) + 2);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = (byte)((int)PacketType | 0b0010);
            m = m.Slice(1);

            m = m.Slice(EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(PacketId >> 8);
            m[1] = (byte)(PacketId & 0x00ff);
            m = m.Slice(2);

            foreach(var topic in Topics)
            {
                m = m.Slice(EncodeString(topic, m));
            }

            return buffer;
        }
    }
}