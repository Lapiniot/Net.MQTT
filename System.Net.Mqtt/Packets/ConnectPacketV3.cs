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
        protected internal const string MqttProtocolName = "MQIsdp";
        protected internal const int MqttProtocolLevel = 0x03;

        public ConnectPacketV3(string clientId, ushort keepAlive = 120, bool cleanSession = true,
            string userName = null, string password = null,
            string willTopic = null, Memory<byte> willMessage = default,
            QoSLevel willQoS = default, bool willRetain = default) :
            base(clientId, MqttProtocolLevel, MqttProtocolName, keepAlive, cleanSession,
                userName, password, willTopic, willMessage, willQoS, willRetain)
        {
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out ConnectPacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            packet = null;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               (PacketType)flags == Connect && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                if(!TryReadString(source, out var protocol, out var consumed) || protocol != MqttProtocolName) return false;
                source = source.Slice(consumed);

                if(!TryReadByte(source, out var level) || level != MqttProtocolLevel) return false;
                source = source.Slice(1);

                if(!TryReadByte(source, out var connFlags)) return false;
                source = source.Slice(1);

                if(!TryReadUInt16(source, out var keepAlive)) return false;
                source = source.Slice(2);

                if(!TryReadString(source, out var clientId, out consumed)) return false;
                source = source.Slice(consumed);

                string topic = null;
                byte[] willMessage = null;
                if((connFlags & 0b0000_0100) == 0b0000_0100)
                {
                    if(!TryReadString(source, out topic, out consumed) || consumed <= 2) return false;
                    source = source.Slice(consumed);

                    if(!TryReadUInt16(source, out var len)) return false;

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
                    if(!TryReadString(source, out userName, out consumed) || consumed <= 2) return false;

                    source = source.Slice(consumed);
                }

                string password = null;
                if((connFlags & 0b0100_0000) == 0b0100_0000)
                {
                    if(!TryReadString(source, out password, out consumed) || consumed <= 2) return false;
                }

                packet = new ConnectPacketV3(clientId, keepAlive,
                    (connFlags & 0b0000_0010) == 0b0000_0010,
                    userName, password, topic, willMessage,
                    (QoSLevel)((connFlags >> 3) & QoSMask),
                    (connFlags & 0b0010_0000) == 0b0010_0000);

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out ConnectPacket packet)
        {
            packet = null;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               (PacketType)flags == Connect && offset + length <= source.Length)
            {
                source = source.Slice(offset, length);

                var len = ReadUInt16BigEndian(source);

                if(UTF8.GetString(source.Slice(2, len)) != MqttProtocolName) return false;

                source = source.Slice(len + 2);

                if(source[0] != MqttProtocolLevel) return false;

                source = source.Slice(1);

                var connFlags = source[0];

                source = source.Slice(1);

                var keepAlive = ReadUInt16BigEndian(source);

                source = source.Slice(2);

                len = ReadUInt16BigEndian(source);

                string clientId = null;

                if(len > 0)
                {
                    clientId = UTF8.GetString(source.Slice(2, len));
                }

                source = source.Slice(len + 2);

                string willTopic = null;
                byte[] willMessage = default;
                if((connFlags & 0b0000_0100) == 0b0000_0100)
                {
                    len = ReadUInt16BigEndian(source);

                    if(len == 0)
                    {
                        packet = null;
                        return false;
                    }

                    willTopic = UTF8.GetString(source.Slice(2, len));

                    source = source.Slice(len + 2);

                    len = ReadUInt16BigEndian(source);

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
                    len = ReadUInt16BigEndian(source);
                    userName = UTF8.GetString(source.Slice(2, len));
                    source = source.Slice(len + 2);
                }

                string password = null;
                if((connFlags & 0b0100_0000) == 0b0100_0000)
                {
                    len = ReadUInt16BigEndian(source);
                    password = UTF8.GetString(source.Slice(2, len));
                }

                packet = new ConnectPacketV3(clientId, keepAlive,
                    (connFlags & 0b0000_0010) == 0b0000_0010,
                    userName, password, willTopic, willMessage,
                    (QoSLevel)((connFlags >> 3) & QoSMask),
                    (connFlags & 0b0010_0000) == 0b0010_0000);
                return true;
            }

            return false;
        }
    }
}