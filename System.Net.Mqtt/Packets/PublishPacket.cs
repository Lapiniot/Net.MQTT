using System.Buffers;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.MqttHelpers;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets
{
    public sealed class PublishPacket : MqttPacket
    {
        public PublishPacket(string topic, Memory<byte> payload = default)
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

            var headerSize = 2 + (shouldContainPacketId ? 2 : 0) + UTF8.GetByteCount(Topic);
            var length = headerSize + Payload.Length;
            var buffer = new byte[1 + GetLengthByteCount(length) + length];

            Span<byte> m = buffer;
            var flags = (byte)((byte)Publish | ((byte)QoSLevel << 1));
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

        public static bool TryParse(ReadOnlySpan<byte> source, out PublishPacket packet)
        {
            packet = null;

            if(TryParseHeader(source, out var flags, out var length, out var offset)
               && ((PacketType)flags & Publish) == Publish &&
               offset + length <= source.Length)
            {
                packet = new PublishPacket
                {
                    Retain = (flags & PacketFlags.Retain) == PacketFlags.Retain,
                    Duplicate = (flags & PacketFlags.Duplicate) == PacketFlags.Duplicate,
                    QoSLevel = (QoSLevel)((flags & QoSMask) >> 1)
                };

                var packetIdLength = packet.QoSLevel != AtMostOnce ? 2 : 0;

                source = source.Slice(offset);

                var topicLength = ReadUInt16BigEndian(source);

                if(source.Length < topicLength + 2 + packetIdLength) return false;

                packet.Topic = UTF8.GetString(source.Slice(2, topicLength));

                source = source.Slice(2 + topicLength);

                if(packetIdLength > 0)
                {
                    packet.PacketId = ReadUInt16BigEndian(source);
                    source = source.Slice(2);
                }

                packet.Payload = source.ToArray();

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out PublishPacket packet)
        {
            packet = null;

            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            if(TryParseHeader(source, out var flags, out var length, out var offset)
               && ((PacketType)flags & Publish) == Publish &&
               offset + length <= source.Length)
            {
                packet = new PublishPacket
                {
                    Retain = (flags & PacketFlags.Retain) == PacketFlags.Retain,
                    Duplicate = (flags & PacketFlags.Duplicate) == PacketFlags.Duplicate,
                    QoSLevel = (QoSLevel)((flags & QoSMask) >> 1)
                };

                source = source.Slice(offset);

                if(TryReadUInt16(source, out var topicLength) && source.Length - 2 >= topicLength)
                {
                    source = source.Slice(2);

                    packet.Topic = source.First.Length >= topicLength
                        ? UTF8.GetString(source.First.Span.Slice(0, topicLength))
                        : UTF8.GetString(source.Slice(0, topicLength).ToArray());

                    source = source.Slice(topicLength);
                }
                else
                {
                    return false;
                }

                var containsPacketId = packet.QoSLevel != AtMostOnce;

                if(containsPacketId)
                {
                    if(TryReadUInt16(source, out var id))
                    {
                        packet.PacketId = id;
                        source = source.Slice(2);
                    }
                    else
                    {
                        return false;
                    }
                }

                packet.Payload = source.ToArray();

                return true;
            }

            return false;
        }
    }
}