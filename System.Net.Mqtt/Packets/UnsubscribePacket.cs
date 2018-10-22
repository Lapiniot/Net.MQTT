using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Packets
{
    public class UnsubscribePacket : MqttPacketWithId
    {
        private const int HeaderValue = (byte)PacketType.Unsubscribe | 0b0010;

        public UnsubscribePacket(ushort id, params string[] topics) : base(id)
        {
            Topics = new List<string>(topics);
        }

        public List<string> Topics { get; }

        protected override byte Header => HeaderValue;

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t) + 2);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = HeaderValue;
            m = m.Slice(1);

            m = m.Slice(EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var topic in Topics)
            {
                m = m.Slice(EncodeString(topic, m));
            }

            return buffer;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out UnsubscribePacket packet)
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

                packet = new UnsubscribePacket(id);

                while(TryReadString(source, out var topic, out var consumed))
                {
                    source = source.Slice(consumed);

                    packet.Topics.Add(topic);
                }

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out UnsubscribePacket packet)
        {
            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);
                packet = new UnsubscribePacket(ReadUInt16BigEndian(source));
                source = source.Slice(2);

                while(TryReadString(source, out var topic, out var consumed))
                {
                    packet.Topics.Add(topic);
                    source = source.Slice(consumed);
                }

                return true;
            }

            packet = null;
            return false;
        }
    }
}