using System.Buffers;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Packets
{
    public class SubAckPacket : MqttPacketWithId
    {
        private const byte HeaderValue = (byte)PacketType.SubAck;

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
            Memory<byte> buffer = new byte[1 + GetLengthByteCount(payloadSize) + payloadSize];
            var span = buffer.Span;

            span[0] = HeaderValue;
            span = span.Slice(1);

            span = span.Slice(EncodeLengthBytes(payloadSize, span));

            WriteUInt16BigEndian(span, Id);
            span = span.Slice(2);

            Result.CopyTo(span);

            return buffer;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out SubAckPacket packet)
        {
            if(source.IsSingleSegment) return TryParse(source.First.Span, out packet);

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                return TryParsePayload(source, out packet);
            }

            packet = null;
            return false;
        }

        public static bool TryParsePayload(ReadOnlySequence<byte> source, out SubAckPacket packet)
        {
            if(source.Length <= 2 || !TryReadUInt16(source, out var id))
            {
                packet = null;
                return false;
            }

            packet = new SubAckPacket(id, source.Slice(2, source.Length - 2).ToArray());
            return true;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out SubAckPacket packet)
        {
            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               flags == HeaderValue && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);
                return TryParsePayload(source, out packet);
            }

            packet = null;
            return false;
        }

        public static bool TryParsePayload(ReadOnlySpan<byte> source, out SubAckPacket packet)
        {
            if(source.Length <= 2)
            {
                packet = null;
                return false;
            }

            packet = new SubAckPacket(ReadUInt16BigEndian(source), source.Slice(2, source.Length - 2).ToArray());
            return true;
        }
    }
}