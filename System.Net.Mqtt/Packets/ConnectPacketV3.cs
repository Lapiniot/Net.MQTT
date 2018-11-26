using System.Buffers;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets
{
    public class ConnectPacketV3 : ConnectPacket
    {
        public const int Level = 0x03;

        public ConnectPacketV3(string clientId, string protocolName = "MQIsdp",
            ushort keepAlive = 120, bool cleanSession = true,
            string userName = null, string password = null,
            string willTopic = null, Memory<byte> willMessage = default,
            byte willQoS = default, bool willRetain = default) :
            base(clientId, Level, protocolName, keepAlive, cleanSession,
                userName, password, willTopic, willMessage, willQoS, willRetain)
        {
        }

        private ConnectPacketV3(string clientId, byte protocolLevel, string protocolName,
            ushort keepAlive, bool cleanSession, string userName, string password,
            string willTopic, Memory<byte> willMessage, byte willQoS, bool willRetain) :
            base(clientId, protocolLevel, protocolName, keepAlive, cleanSession, userName, password,
                willTopic, willMessage, willQoS, willRetain)
        {
        }

        public static bool TryParse(ReadOnlySequence<byte> sequence, out ConnectPacketV3 packet, out int consumed)
        {
            // Fast path
            if(sequence.IsSingleSegment) return TryParse(sequence.First.Span, out packet, out consumed);

            packet = null;
            consumed = 0;

            if(!TryParseHeader(sequence, out var header, out var length, out var offset) || offset + length > sequence.Length) return false;

            if(header != 0b0001_0000 || !TryParsePayload(sequence.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out ConnectPacketV3 packet, out int consumed)
        {
            packet = null;
            consumed = 0;

            if(!TryParseHeader(source, out var flags, out var length, out var offset) || offset + length > source.Length) return false;

            if(flags != 0b0001_0000 || !TryParsePayload(source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParsePayload(ReadOnlySequence<byte> source, out ConnectPacketV3 packet)
        {
            if(source.IsSingleSegment) return TryParsePayload(source.First.Span, out packet);

            packet = null;

            if(!TryReadString(source, out var protocol, out var len)) return false;
            source = source.Slice(len);

            if(!TryReadByte(source, out var level)) return false;
            source = source.Slice(1);

            if(!TryReadByte(source, out var connFlags)) return false;
            source = source.Slice(1);

            if(!TryReadUInt16(source, out var keepAlive)) return false;
            source = source.Slice(2);

            if(!TryReadString(source, out var clientId, out len)) return false;
            source = source.Slice(len);

            string topic = null;
            byte[] willMessage = null;
            if((connFlags & 0b0000_0100) == 0b0000_0100)
            {
                if(!TryReadString(source, out topic, out len) || len <= 2) return false;
                source = source.Slice(len);

                if(!TryReadUInt16(source, out var size)) return false;

                if(size > 0)
                {
                    willMessage = new byte[size];
                    source.Slice(2, size).CopyTo(willMessage);
                }

                source = source.Slice(size + 2);
            }

            string userName = null;
            if((connFlags & 0b1000_0000) == 0b1000_0000)
            {
                if(!TryReadString(source, out userName, out len) || len <= 2) return false;

                source = source.Slice(len);
            }

            string password = null;
            if((connFlags & 0b0100_0000) == 0b0100_0000)
            {
                if(!TryReadString(source, out password, out len) || len <= 2) return false;
            }

            packet = new ConnectPacketV3(clientId, level, protocol, keepAlive,
                (connFlags & 0b0010) == 0b0010, userName, password, topic, willMessage,
                (byte)((connFlags >> 3) & QoSMask), (connFlags & 0b0010_0000) == 0b0010_0000);

            return true;
        }

        public static bool TryParsePayload(ReadOnlySpan<byte> payload, out ConnectPacketV3 packet)
        {
            packet = null;

            if(!TryReadUInt16BigEndian(payload, out var len) || payload.Length < len + 8) return false;

            var protocol = UTF8.GetString(payload.Slice(2, len));
            payload = payload.Slice(len + 2);

            var level = payload[0];
            payload = payload.Slice(1);

            var connFlags = payload[0];
            payload = payload.Slice(1);

            var keepAlive = ReadUInt16BigEndian(payload);
            payload = payload.Slice(2);

            len = ReadUInt16BigEndian(payload);
            string clientId = null;
            if(len > 0)
            {
                if(payload.Length < len + 2) return false;
                clientId = UTF8.GetString(payload.Slice(2, len));
            }

            payload = payload.Slice(len + 2);

            string willTopic = null;
            byte[] willMessage = default;
            if((connFlags & 0b0000_0100) == 0b0000_0100)
            {
                if(!TryReadUInt16BigEndian(payload, out len) || len == 0 || payload.Length < len + 2) return false;
                willTopic = UTF8.GetString(payload.Slice(2, len));
                payload = payload.Slice(len + 2);

                if(!TryReadUInt16BigEndian(payload, out len) || payload.Length < len + 2) return false;
                if(len > 0)
                {
                    willMessage = new byte[len];
                    payload.Slice(2, len).CopyTo(willMessage);
                }

                payload = payload.Slice(len + 2);
            }

            string userName = null;
            if((connFlags & 0b1000_0000) == 0b1000_0000)
            {
                if(!TryReadUInt16BigEndian(payload, out len) || payload.Length < len + 2) return false;
                userName = UTF8.GetString(payload.Slice(2, len));
                payload = payload.Slice(len + 2);
            }

            string password = null;
            if((connFlags & 0b0100_0000) == 0b0100_0000)
            {
                if(!TryReadUInt16BigEndian(payload, out len) || payload.Length < len + 2) return false;
                password = UTF8.GetString(payload.Slice(2, len));
            }

            packet = new ConnectPacketV3(clientId, level, protocol, keepAlive,
                (connFlags & 0x2) == 0x2, userName, password, willTopic, willMessage,
                (byte)((connFlags >> 3) & QoSMask), (connFlags & 0x2_0) == 0x2_0);

            return true;
        }
    }
}