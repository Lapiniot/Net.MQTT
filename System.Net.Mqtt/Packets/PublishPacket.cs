using System.Buffers;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets
{
    public sealed class PublishPacket : MqttPacket
    {
        public PublishPacket(ushort id, QoSLevel qoSLevel, string topic,
            Memory<byte> payload = default, bool retain = false, bool duplicate = false)
        {
            if(id == 0 && qoSLevel != AtMostOnce) throw new ArgumentException(MissingPacketId, nameof(id));
            if(IsNullOrEmpty(topic)) throw new ArgumentException(NotEmptyStringExpected, nameof(topic));

            Id = id;
            QoSLevel = qoSLevel;
            Topic = topic;
            Payload = payload;
            Retain = retain;
            Duplicate = duplicate;
        }

        public QoSLevel QoSLevel { get; }
        public bool Retain { get; }
        public bool Duplicate { get; }
        public string Topic { get; }
        public ushort Id { get; }
        public Memory<byte> Payload { get; }

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
                WriteUInt16BigEndian(m, Id);

                m = m.Slice(2);
            }

            Payload.Span.CopyTo(m);

            return buffer;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out PublishPacket packet, out int consumed)
        {
            packet = null;
            consumed = 0;

            if(!TryParseHeader(source, out var header, out var length, out var offset) || offset + length > source.Length) return false;

            if((header & 0b11_0000) != 0b11_0000 || !TryParsePayload(header, source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out PublishPacket packet, out int consumed)
        {
            if(source.IsSingleSegment) return TryParse(source.First.Span, out packet, out consumed);

            packet = null;
            consumed = 0;

            if(!TryParseHeader(source, out var header, out var length, out var offset) || offset + length > source.Length) return false;

            if((header & 0b11_0000) != 0b11_0000 || !TryParsePayload(header, source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParsePayload(byte header, ReadOnlySequence<byte> source, out PublishPacket packet)
        {
            if(source.IsSingleSegment) return TryParsePayload(header, source.First.Span, out packet);

            packet = null;

            var qosLevel = (QoSLevel)((header >> 1) & QoSMask);

            var packetIdLength = qosLevel != AtMostOnce ? 2 : 0;

            if(!TryReadUInt16(source, out var topicLength) || source.Length < topicLength + 2 + packetIdLength) return false;

            var topic = source.First.Length >= topicLength
                ? UTF8.GetString(source.First.Span.Slice(2, topicLength))
                : UTF8.GetString(source.Slice(2, topicLength).ToArray());

            source = source.Slice(topicLength + 2);

            ushort id = 0;
            if(packetIdLength > 0)
            {
                if(!TryReadUInt16(source, out id)) return false;

                source = source.Slice(2);
            }

            packet = new PublishPacket(id, qosLevel, topic, source.ToArray(),
                (header & PacketFlags.Retain) == PacketFlags.Retain,
                (header & PacketFlags.Duplicate) == PacketFlags.Duplicate);

            return true;
        }

        public static bool TryParsePayload(byte header, ReadOnlySpan<byte> source, out PublishPacket packet)
        {
            packet = null;

            var qosLevel = (QoSLevel)((header >> 1) & QoSMask);

            var packetIdLength = qosLevel != AtMostOnce ? 2 : 0;

            var topicLength = ReadUInt16BigEndian(source);

            if(source.Length < topicLength + 2 + packetIdLength) return false;

            var topic = UTF8.GetString(source.Slice(2, topicLength));

            source = source.Slice(2 + topicLength);

            ushort id = 0;

            if(packetIdLength > 0)
            {
                id = ReadUInt16BigEndian(source);
                source = source.Slice(2);
            }

            packet = new PublishPacket(id, qosLevel, topic, source.ToArray(),
                (header & PacketFlags.Retain) == PacketFlags.Retain,
                (header & PacketFlags.Duplicate) == PacketFlags.Duplicate);

            return true;
        }
    }
}