using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Properties;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Net.Mqtt.Packets
{
    public class UnsubscribePacket : MqttPacketWithId
    {
        public UnsubscribePacket(ushort id, params string[] topics) : base(id)
        {
            Topics = topics ?? throw new ArgumentNullException(nameof(topics));
            if(topics.Length == 0) throw new ArgumentException(Strings.NotEmptyCollectionExpected);
        }

        public string[] Topics { get; }

        protected override byte Header => 0b10100010;

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t) + 2);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + SpanExtensions.GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = 0b10100010;
            m = m.Slice(1);

            m = m.Slice(SpanExtensions.EncodeMqttLengthBytes(remainingLength, m));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var topic in Topics)
            {
                m = m.Slice(SpanExtensions.EncodeMqttString(topic, m));
            }

            return buffer;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out UnsubscribePacket packet, out int consumed)
        {
            if(source.IsSingleSegment) return TryParse(source.First.Span, out packet, out consumed);

            consumed = 0;
            packet = null;

            if(!source.TryReadMqttHeader(out var header, out var length, out var offset) || offset + length > source.Length) return false;

            if(header != 0b10100010 || !TryParsePayload(source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out UnsubscribePacket packet, out int consumed)
        {
            consumed = 0;
            packet = null;

            if(!source.TryReadMqttHeader(out var header, out var length, out var offset) || offset + length > source.Length) return false;

            if(header != 0b10100010 || !TryParsePayload(source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParsePayload(ReadOnlySequence<byte> source, out UnsubscribePacket packet)
        {
            if(source.IsSingleSegment) return TryParsePayload(source.First.Span, out packet);

            packet = null;

            if(!source.TryReadUInt16(out var id)) return false;

            source = source.Slice(2);

            var topics = new List<string>();
            while(source.TryReadMqttString(out var topic, out var len))
            {
                source = source.Slice(len);
                topics.Add(topic);
            }

            packet = new UnsubscribePacket(id, topics.ToArray());

            return true;
        }

        public static bool TryParsePayload(ReadOnlySpan<byte> source, out UnsubscribePacket packet)
        {
            var id = ReadUInt16BigEndian(source);
            source = source.Slice(2);

            var topics = new List<string>();
            while(source.TryReadMqttString(out var topic, out var consumed))
            {
                topics.Add(topic);
                source = source.Slice(consumed);
            }

            packet = new UnsubscribePacket(id, topics.ToArray());
            return true;
        }
    }
}