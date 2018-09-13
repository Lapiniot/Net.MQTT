﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net.Mqtt.Packets
{
    public class UnsubscribePacket : MqttPacketWithId
    {
        public UnsubscribePacket(ushort id, params string[] topics) : base(id)
        {
            if(id == 0) throw new ArgumentException($"{nameof(id)} cannot have value of 0");

            Topics = new List<string>(topics);
        }

        public List<string> Topics { get; }

        protected override PacketType PacketType
        {
            get { return PacketType.Unsubscribe; }
        }

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t) + 2);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + MqttHelpers.GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = (byte)((int)PacketType | 0b0010);
            m = m.Slice(1);

            m = m.Slice(MqttHelpers.EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var topic in Topics)
            {
                m = m.Slice(MqttHelpers.EncodeString(topic, m));
            }

            return buffer;
        }
    }
}