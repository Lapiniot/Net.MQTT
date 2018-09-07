using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Packets
{
    public sealed class PublishPacket : MqttPacket
    {
        public PublishPacket(string topic, Memory<byte> payload)
        {
            if(string.IsNullOrEmpty(topic)) throw new ArgumentException("Should not be null or empty", nameof(topic));

            Topic = topic;
            Payload = payload;
        }

        private PublishPacket()
        {
        }

        public string Topic { get; set; }
        public ushort PacketId { get; set; }
        public Memory<byte> Payload { get; set; }

        public override Memory<byte> GetBytes()
        {
            var shouldContainPacketId = QoSLevel != AtMostOnce;

            var headerSize = 2 + (shouldContainPacketId ? 2 : 0) + Encoding.UTF8.GetByteCount(Topic);
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
                BinaryPrimitives.WriteUInt16BigEndian(m, PacketId);

                m = m.Slice(2);
            }

            Payload.Span.CopyTo(m);

            return buffer;
        }

        public static bool TryParse(ReadOnlySequence<byte> buffer, out PublishPacket packet)
        {
            packet = null;

            if(TryParseHeader(buffer, out var flags, out var length, out var offset)
                && ((PacketType)flags & Publish) == Publish &&
                offset + length <= buffer.Length)
            {
                packet = new PublishPacket()
                {
                    Retain = (flags & PacketFlags.Retain) == PacketFlags.Retain,
                    Duplicate = (flags & PacketFlags.Duplicate) == PacketFlags.Duplicate,
                    QoSLevel = (QoSLevel)((flags & PacketFlags.QoSMask) >> 1)
                };

                buffer = buffer.Slice(offset);

                if(TryReadUInt16(buffer, out var topicLength) && buffer.Length - 2 >= topicLength)
                {
                    buffer = buffer.Slice(2);

                    if(buffer.First.Length >= topicLength)
                    {
                        packet.Topic = System.Text.Encoding.UTF8.GetString(buffer.First.Span.Slice(0, topicLength));
                    }
                    else
                    {
                        packet.Topic = System.Text.Encoding.UTF8.GetString(buffer.Slice(0, topicLength).ToArray());
                    }

                    buffer = buffer.Slice(topicLength);
                }
                else return false;

                bool containsPacketId = packet.QoSLevel != AtMostOnce;

                if(containsPacketId)
                {
                    if(TryReadUInt16(buffer, out var id))
                    {
                        packet.PacketId = id;
                        buffer = buffer.Slice(2);
                    }
                    else return false;
                }

                packet.Payload = buffer.ToArray();

                return true;
            }

            return false;
        }
    }
}