using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Properties;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Packets
{
    public class UnsubscribePacket : MqttPacketWithId
    {
        private const int HeaderValue = 0b10100010;

        public UnsubscribePacket(ushort id, params string[] topics) : base(id)
        {
            Topics = topics ?? throw new ArgumentNullException(nameof(topics));
            if(topics.Length == 0) throw new ArgumentException(Strings.NotEmptyCollectionExpected);
        }

        public string[] Topics { get; }

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

        public static bool TryParse(ReadOnlySequence<byte> source, out UnsubscribePacket packet, out int consumed)
        {
            consumed = 0;

            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet, out consumed);
            }

            packet = null;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                if(!TryReadUInt16(source, out var id)) return false;

                source = source.Slice(2);

                var topics = new List<string>();
                while(TryReadString(source, out var topic, out var len))
                {
                    source = source.Slice(len);

                    topics.Add(topic);
                }

                consumed = offset + length;

                packet = new UnsubscribePacket(id, topics.ToArray());

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out UnsubscribePacket packet, out int consumed)
        {
            consumed = 0;
            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);
                var id = ReadUInt16BigEndian(source);
                source = source.Slice(2);

                var topics = new List<string>();
                while(TryReadString(source, out var topic, out var len))
                {
                    topics.Add(topic);
                    source = source.Slice(len);
                }

                consumed = offset + length;
                packet = new UnsubscribePacket(id, topics.ToArray());
                return true;
            }

            packet = null;
            return false;
        }
    }
}