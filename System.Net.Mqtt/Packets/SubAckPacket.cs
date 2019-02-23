using System.Buffers;
using System.Net.Mqtt.Extensions;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Packets
{
    public class SubAckPacket : MqttPacketWithId
    {
        private const byte HeaderValue = 0b1001_0000;

        public SubAckPacket(ushort id, byte[] result) : base(id)
        {
            if(result == null) throw new ArgumentNullException(nameof(result));
            if(result.Length == 0) throw new ArgumentException(NotEmptyCollectionExpected, nameof(result));
            Result = result;
        }

        protected override byte Header => HeaderValue;

        public byte[] Result { get; }

        public override Memory<byte> GetBytes()
        {
            var payloadSize = 2 + Result.Length;
            var total = 1 + SpanExtensions.GetLengthByteCount(payloadSize) + payloadSize;
            Memory<byte> buffer = new byte[total];
            var span = buffer.Span;

            span[0] = HeaderValue;
            span = span.Slice(1);

            span = span.Slice(SpanExtensions.EncodeMqttLengthBytes(ref span, payloadSize));

            WriteUInt16BigEndian(span, Id);
            span = span.Slice(2);

            Result.CopyTo(span);

            return buffer;
        }

        public override bool TryWrite(in Memory<byte> buffer, out int size)
        {
            var payloadSize = 2 + Result.Length;
            size = 1 + SpanExtensions.GetLengthByteCount(payloadSize) + payloadSize;

            if(buffer.Length < size) return false;

            var span = buffer.Span;
            span[0] = HeaderValue;
            span = span.Slice(1);
            span = span.Slice(SpanExtensions.EncodeMqttLengthBytes(ref span, payloadSize));
            WriteUInt16BigEndian(span, Id);
            span = span.Slice(2);
            Result.CopyTo(span);
            return true;
        }

        public static bool TryRead(ReadOnlySequence<byte> sequence, out SubAckPacket packet)
        {
            if(sequence.IsSingleSegment) return TryRead(sequence.First.Span, out packet);

            var sr = new SequenceReader<byte>(sequence);
            return TryRead(ref sr, out packet);
        }

        public static bool TryRead(ref SequenceReader<byte> reader, out SubAckPacket packet)
        {
            if(reader.Sequence.IsSingleSegment) return TryRead(reader.UnreadSpan, out packet);

            var remaining = reader.Remaining;

            if(reader.TryReadMqttHeader(out var flags, out var size) && flags == HeaderValue && reader.Remaining >= size)
            {
                return TryReadPayload(ref reader, size, out packet);
            }

            reader.Rewind(remaining - reader.Remaining);
            packet = null;
            return false;
        }

        public static bool TryRead(ReadOnlySpan<byte> span, out SubAckPacket packet)
        {
            if(span.TryReadMqttHeader(out var flags, out var size, out var offset) &&
               flags == HeaderValue && offset + size <= span.Length)
            {
                return TryReadPayload(span.Slice(offset), size, out packet);
            }

            packet = null;
            return false;
        }

        public static bool TryReadPayload(ReadOnlySequence<byte> sequence, int size, out SubAckPacket packet)
        {
            packet = null;
            if(sequence.Length < size) return false;
            if(sequence.IsSingleSegment) return TryReadPayload(sequence.First.Span, size, out packet);

            var sr = new SequenceReader<byte>(sequence);
            return TryReadPayload(ref sr, size, out packet);
        }

        public static bool TryReadPayload(ref SequenceReader<byte> reader, int size, out SubAckPacket packet)
        {
            packet = null;
            if(reader.Remaining < size) return false;
            if(reader.Sequence.IsSingleSegment) return TryReadPayload(reader.UnreadSpan, size, out packet);

            if(!reader.TryReadBigEndian(out ushort id)) return false;

            var buffer = new byte[size - 2];

            if(reader.Remaining < buffer.Length || !reader.TryCopyTo(buffer))
            {
                reader.Rewind(2);
                return false;
            }

            packet = new SubAckPacket(id, buffer);

            return true;
        }

        public static bool TryReadPayload(ReadOnlySpan<byte> span, int size, out SubAckPacket packet)
        {
            if(span.Length < size)
            {
                packet = null;
                return false;
            }

            packet = new SubAckPacket(ReadUInt16BigEndian(span), span.Slice(2, size - 2).ToArray());
            return true;
        }
    }
}