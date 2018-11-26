using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Packets
{
    public class SubscribePacket : MqttPacketWithId
    {
        public SubscribePacket(ushort id, params (string, byte)[] topics) : base(id)
        {
            Topics = topics ?? throw new ArgumentNullException(nameof(topics));
            if(topics.Length == 0) throw new ArgumentException(NotEmptyCollectionExpected);
        }

        public (string topic, byte qosLevel)[] Topics { get; }

        protected override byte Header => 0b10000010;

        public override Memory<byte> GetBytes()
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t.topic) + 3);
            var remainingLength = payloadLength + 2;
            var buffer = new byte[1 + GetLengthByteCount(remainingLength) + remainingLength];
            Span<byte> m = buffer;

            m[0] = 0b10000010;
            m = m.Slice(1);

            m = m.Slice(EncodeLengthBytes(remainingLength, m));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var t in Topics)
            {
                m = m.Slice(EncodeString(t.topic, m));
                m[0] = t.qosLevel;
                m = m.Slice(1);
            }

            return buffer;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out SubscribePacket packet, out int consumed)
        {
            if(source.IsSingleSegment) return TryParse(source.First.Span, out packet, out consumed);

            consumed = 0;
            packet = null;

            if(!TryParseHeader(source, out var header, out var length, out var offset) || offset + length > source.Length) return false;

            if(header != 0b10000010 || !TryParsePayload(source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out SubscribePacket packet, out int consumed)
        {
            consumed = 0;
            packet = null;

            if(!TryParseHeader(source, out var header, out var length, out var offset) || offset + length > source.Length) return false;

            if(header != 0b10000010 || !TryParsePayload(source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParsePayload(ReadOnlySequence<byte> source, out SubscribePacket packet)
        {
            if(source.IsSingleSegment) return TryParsePayload(source.First.Span, out packet);

            packet = null;

            if(!TryReadUInt16(source, out var id)) return false;

            source = source.Slice(2);

            var list = new List<(string, byte)>();

            while(TryReadString(source, out var topic, out var len))
            {
                source = source.Slice(len);

                if(!TryReadByte(source, out var qos)) return false;

                source = source.Slice(1);

                list.Add((topic, qos));
            }

            packet = new SubscribePacket(id, list.ToArray());
            return true;
        }

        public static bool TryParsePayload(ReadOnlySpan<byte> source, out SubscribePacket packet)
        {
            var id = ReadUInt16BigEndian(source);
            source = source.Slice(2);

            var list = new List<(string, byte)>();
            while(TryReadString(source, out var topic, out var len))
            {
                list.Add((topic, source[len]));
                source = source.Slice(len + 1);
            }

            packet = new SubscribePacket(id, list.ToArray());

            return true;
        }
    }
}