using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Messages
{
    public class PublishMessage : MqttMessage
    {
        public PublishMessage(string topic, Memory<byte> payload)
        {
            if(string.IsNullOrEmpty(topic)) throw new ArgumentException("Should not be null or empty", nameof(topic));

            Topic = topic;
            Payload = payload;
        }

        public string Topic { get; set; }
        public ushort PacketId { get; set; }
        public Memory<byte> Payload { get; set; }

        public override Memory<byte> GetBytes()
        {
            var shouldContainPacketId = QoSLevel != AtMostOnce;

            var headerSize = 2 + (shouldContainPacketId ? 2 : 0) + UTF8.GetByteCount(Topic);
            var length = headerSize + Payload.Length;
            var buffer = new byte[1 + GetLengthByteCount(length) + length];

            Span<byte> m = buffer;
            var flags = (byte)((byte)PacketType.Publish | ((byte)QoSLevel << 1));
            if(Retain) flags |= PacketFlags.Retain;
            if(Duplicate) flags |= PacketFlags.Duplicate;
            m[0] = flags;
            m = m.Slice(1);

            m = m.Slice(EncodeLengthBytes(length, m));

            m = m.Slice(EncodeString(Topic, m));

            if(shouldContainPacketId)
            {
                WriteUInt16BigEndian(m, PacketId);

                m = m.Slice(2);
            }

            Payload.Span.CopyTo(m);

            return buffer;
        }
    }
}