using System.Buffers.Binary;

namespace System.Net.Mqtt.Packets
{
    public class SubAckPacket : MqttPacketWithId
    {
        private const byte HeaderValue = (byte)PacketType.SubAck;
        private readonly byte[] result;

        public SubAckPacket(ushort id, byte[] result) : base(id)
        {
            this.result = result;
        }

        protected override byte Header => HeaderValue;

        public override Memory<byte> GetBytes()
        {
            var payloadSize = 2 + result.Length;
            Memory<byte> buffer = new byte[1 + MqttHelpers.GetLengthByteCount(payloadSize) + payloadSize];
            var span = buffer.Span;
            span[0] = HeaderValue;
            span = span.Slice(1);
            span = span.Slice(MqttHelpers.EncodeLengthBytes(payloadSize, span));
            BinaryPrimitives.WriteUInt16BigEndian(span, Id);
            span = span.Slice(2);
            result.CopyTo(span);
            return buffer;
        }
    }
}