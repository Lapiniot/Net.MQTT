using System.Buffers;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
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
            QoSLevel willQoS = default, bool willRetain = default) :
            base(clientId, Level, protocolName, keepAlive, cleanSession,
                userName, password, willTopic, willMessage, willQoS, willRetain)
        {
        }

        private ConnectPacketV3(string clientId, byte protocolLevel, string protocolName,
            ushort keepAlive, bool cleanSession, string userName, string password,
            string willTopic, Memory<byte> willMessage, QoSLevel willQoS, bool willRetain) :
            base(clientId, protocolLevel, protocolName, keepAlive, cleanSession, userName, password,
                willTopic, willMessage, willQoS, willRetain)
        {
        }

        public static bool TryParse(ReadOnlySequence<byte> source, bool strict, out ConnectPacketV3 packet, out int consumed)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, strict, out packet, out consumed);
            }

            packet = null;
            consumed = 0;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               (PacketType)flags == Connect && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                if(!TryReadString(source, out var protocol, out var len)) return false;
                source = source.Slice(len);

                if(!TryReadByte(source, out var level) || strict && level != Level) return false;
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
                    (QoSLevel)((connFlags >> 3) & QoSMask), (connFlags & 0b0010_0000) == 0b0010_0000);

                consumed = offset + length;

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, bool strict, out ConnectPacketV3 packet, out int consumed)
        {
            packet = null;
            consumed = 0;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               (PacketType)flags == Connect && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                if(!TryReadUInt16BigEndian(source, out var len) || source.Length < len + 8) return false;
                var protocol = UTF8.GetString(source.Slice(2, len));
                source = source.Slice(len + 2);

                var level = source[0];
                if(strict && level != Level) return false;
                source = source.Slice(1);

                var connFlags = source[0];
                source = source.Slice(1);

                var keepAlive = ReadUInt16BigEndian(source);
                source = source.Slice(2);

                len = ReadUInt16BigEndian(source);
                string clientId = null;
                if(len > 0)
                {
                    if(source.Length < len + 2) return false;
                    clientId = UTF8.GetString(source.Slice(2, len));
                }
                source = source.Slice(len + 2);

                string willTopic = null;
                byte[] willMessage = default;
                if((connFlags & 0b0000_0100) == 0b0000_0100)
                {
                    if(!TryReadUInt16BigEndian(source, out len) || len == 0 || source.Length < len + 2) return false;
                    willTopic = UTF8.GetString(source.Slice(2, len));
                    source = source.Slice(len + 2);

                    if(!TryReadUInt16BigEndian(source, out len) || source.Length < len + 2) return false;
                    if(len > 0)
                    {
                        willMessage = new byte[len];
                        source.Slice(2, len).CopyTo(willMessage);
                    }
                    source = source.Slice(len + 2);
                }

                string userName = null;
                if((connFlags & 0b1000_0000) == 0b1000_0000)
                {
                    if(!TryReadUInt16BigEndian(source, out len) || source.Length < len + 2) return false;
                    userName = UTF8.GetString(source.Slice(2, len));
                    source = source.Slice(len + 2);
                }

                string password = null;
                if((connFlags & 0b0100_0000) == 0b0100_0000)
                {
                    if(!TryReadUInt16BigEndian(source, out len) || source.Length < len + 2) return false;
                    password = UTF8.GetString(source.Slice(2, len));
                }

                packet = new ConnectPacketV3(clientId, level, protocol, keepAlive,
                    (connFlags & 0x2) == 0x2, userName, password, willTopic, willMessage,
                    (QoSLevel)((connFlags >> 3) & QoSMask), (connFlags & 0x2_0) == 0x2_0);

                consumed = offset + length;

                return true;
            }

            return false;
        }
    }
}