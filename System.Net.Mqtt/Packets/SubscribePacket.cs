using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Packets
{
    public class SubscribePacket : MqttPacketWithId
    {
        protected internal const int HeaderValue = (byte)PacketType.Subscribe | 0b0010;

        public SubscribePacket(ushort id, params (string, QoSLevel)[] topics) : base(id)
        {
            if(id == 0) throw new ArgumentException($"{nameof(id)} cannot have value of 0");

            Topics = new List<(string, QoSLevel)>(topics);
        }

        public List<(string topic, QoSLevel qosLevel)> Topics { get; }

        protected override byte Header => HeaderValue;

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t.topic) + 3);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = HeaderValue;
            m = m.Slice(1);

            m = m.Slice(EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var t in Topics)
            {
                m = m.Slice(EncodeString(t.topic, m));
                m[0] = (byte)t.qosLevel;
                m = m.Slice(1);
            }

            return buffer;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out SubscribePacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            packet = null;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                if(!TryReadUInt16(source, out var id)) return false;

                source = source.Slice(2);

                packet = new SubscribePacket(id);

                while(TryReadString(source, out var topic, out var consumed))
                {
                    source = source.Slice(consumed);

                    if(!TryReadByte(source, out var qos))
                    {
                        packet = null;
                        return false;
                    }

                    source = source.Slice(1);

                    packet.Topics.Add((topic, (QoSLevel)qos));
                }

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out SubscribePacket packet)
        {
            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);
                packet = new SubscribePacket(ReadUInt16BigEndian(source));
                source = source.Slice(2);

                while(TryReadString(source, out var topic, out var consumed))
                {
                    packet.Topics.Add((topic, (QoSLevel)source[consumed]));
                    source = source.Slice(consumed + 1);
                }

                return true;
            }

            packet = null;
            return false;
        }
    }
}