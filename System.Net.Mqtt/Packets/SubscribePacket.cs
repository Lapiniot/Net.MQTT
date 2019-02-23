using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Extensions;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
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
            var size = 1 + SpanExtensions.GetLengthByteCount(remainingLength) + remainingLength;
            var buffer = new byte[size];
            Span<byte> m = buffer;

            m[0] = 0b10000010;
            m = m.Slice(1);

            m = m.Slice(SpanExtensions.EncodeMqttLengthBytes(ref m, remainingLength));
            m[0] = (byte)(Id >> 8);
            m[1] = (byte)(Id & 0x00ff);
            m = m.Slice(2);

            foreach(var (topic, qosLevel) in Topics)
            {
                m = m.Slice(SpanExtensions.EncodeMqttString(ref m, topic));
                m[0] = qosLevel;
                m = m.Slice(1);
            }

            return buffer;
        }

        public override bool TryWrite(in Memory<byte> buffer, out int size)
        {
            var payloadLength = Topics.Sum(t => Encoding.UTF8.GetByteCount(t.topic) + 3);
            var remainingLength = payloadLength + 2;
            size = 1 + SpanExtensions.GetLengthByteCount(remainingLength) + remainingLength;
            if(size > buffer.Length) return false;

            var span = buffer.Span;
            span[0] = 0b10000010;
            span = span.Slice(1);
            span = span.Slice(SpanExtensions.EncodeMqttLengthBytes(ref span, remainingLength));
            WriteUInt16BigEndian(span, Id);
            span = span.Slice(2);

            foreach(var (topic, qosLevel) in Topics)
            {
                span = span.Slice(SpanExtensions.EncodeMqttString(ref span, topic));
                span[0] = qosLevel;
                span = span.Slice(1);
            }

            return true;
        }

        public static bool TryRead(ReadOnlySequence<byte> sequence, out SubscribePacket packet, out int consumed)
        {
            if(sequence.IsSingleSegment) return TryRead(sequence.First.Span, out packet, out consumed);

            var sr = new SequenceReader<byte>(sequence);
            return TryRead(ref sr, out packet, out consumed);
        }

        public static bool TryRead(ref SequenceReader<byte> reader, out SubscribePacket packet, out int consumed)
        {
            if(reader.Sequence.IsSingleSegment) return TryRead(reader.UnreadSpan, out packet, out consumed);

            consumed = 0;
            packet = null;
            var remaining = reader.Remaining;

            if(!reader.TryReadMqttHeader(out var header, out var size) || size > reader.Remaining ||
               header != 0b10000010 || !TryReadPayload(ref reader, size, out packet))
            {
                reader.Rewind(remaining - reader.Remaining);
                return false;
            }

            consumed = (int)(remaining - reader.Remaining);
            return true;
        }

        public static bool TryRead(ReadOnlySpan<byte> span, out SubscribePacket packet, out int consumed)
        {
            consumed = 0;
            packet = null;

            if(!span.TryReadMqttHeader(out var header, out var size, out var offset) || offset + size > span.Length ||
               header != 0b10000010 || !TryReadPayload(span.Slice(offset), size, out packet))
            {
                return false;
            }

            consumed = offset + size;
            return true;
        }

        public static bool TryReadPayload(ReadOnlySequence<byte> sequence, int size, out SubscribePacket packet)
        {
            packet = null;
            if(sequence.Length < size) return false;
            if(sequence.IsSingleSegment) return TryReadPayload(sequence.First.Span, size, out packet);

            var sr = new SequenceReader<byte>(sequence);
            return TryReadPayload(ref sr, size, out packet);
        }

        public static bool TryReadPayload(ref SequenceReader<byte> reader, int size, out SubscribePacket packet)
        {
            packet = null;
            if(reader.Remaining < size) return false;
            if(reader.Sequence.IsSingleSegment) return TryReadPayload(reader.UnreadSpan, size, out packet);

            var remaining = reader.Remaining;

            if(!reader.TryReadBigEndian(out ushort id)) return false;

            var list = new List<(string, byte)>();

            while(remaining - reader.Remaining < size && reader.TryReadMqttString(out var topic))
            {
                if(!reader.TryRead(out var qos)) return false;
                list.Add((topic, qos));
            }

            var consumed = remaining - reader.Remaining;
            if(consumed < size)
            {
                reader.Rewind(consumed);
                return false;
            }

            packet = new SubscribePacket(id, list.ToArray());
            return true;
        }

        public static bool TryReadPayload(ReadOnlySpan<byte> span, int size, out SubscribePacket packet)
        {
            packet = null;
            if(span.Length < size) return false;
            if(span.Length > size) span = span.Slice(0, size);

            var id = ReadUInt16BigEndian(span);
            span = span.Slice(2);

            var list = new List<(string, byte)>();
            while(span.TryReadMqttString(out var topic, out var len))
            {
                list.Add((topic, span[len]));
                span = span.Slice(len + 1);
            }

            if(span.Length > 0) return false;

            packet = new SubscribePacket(id, list.ToArray());

            return true;
        }
    }
}