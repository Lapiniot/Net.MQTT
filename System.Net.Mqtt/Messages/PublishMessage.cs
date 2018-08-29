using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Messages
{
    public class PublishMessage : MqttMessage
    {
        public string Topic { get; set; }
        public ushort PacketId { get; set; }
        public Memory<byte> Payload { get; set; }
        public override Memory<byte> GetBytes()
        {
            var headerSize = 4 + UTF8.GetByteCount(Topic);
            var length = headerSize + Payload.Length;
            var buffer = new byte[1 + GetLengthByteCount(length) + length];

            Span<byte> m = buffer;
            byte flags = (byte)((byte)PacketType.Publish | ((byte)QoSLevel << 1));
            if(Retain) flags |= PacketFlags.Retain;
            if(Duplicate) flags |= PacketFlags.Duplicate;
            m[0] = flags;

            m = m.Slice(EncodeLengthBytes(length, m));

            m = m.Slice(EncodeString(Topic, m));

            WriteUInt16BigEndian(m, PacketId);

            m = m.Slice(2);

            Payload.Span.CopyTo(m);

            return buffer;
        }
    }
}