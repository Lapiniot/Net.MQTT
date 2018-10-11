using System.Buffers;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
using static System.String;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets
{
    public sealed class ConnectPacket : MqttPacket
    {
        public ConnectPacket(string clientId)
        {
            ClientId = clientId;
        }

        public ushort KeepAlive { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public string WillTopic { get; set; }
        public Memory<byte> WillMessage { get; set; }
        public QoSLevel WillQoS { get; set; }
        public bool WillRetain { get; set; }
        public bool CleanSession { get; set; } = true;
        public string ProtocolName { get; set; } = "MQIsdp";
        public byte ProtocolLevel { get; set; } = 0x03;

        public override Memory<byte> GetBytes()
        {
            var hasClientId = !IsNullOrEmpty(ClientId);
            var hasUserName = !IsNullOrEmpty(UserName);
            var hasPassword = !IsNullOrEmpty(Password);
            var hasWillTopic = !IsNullOrEmpty(WillTopic);

            var length = GetHeaderSize() + GetPayloadSize();

            var buffer = new byte[1 + GetLengthByteCount(length) + length];

            var mem = (Span<byte>)buffer;

            // Packet flags
            mem[0] = (byte)Connect;
            mem = mem.Slice(1);

            // Remaining length bytes
            mem = mem.Slice(EncodeLengthBytes(length, mem));

            // Protocol info bytes
            mem = mem.Slice(EncodeString(ProtocolName, mem));
            mem[0] = ProtocolLevel;
            mem = mem.Slice(1);


            // Connection flag
            var flags = (byte)((byte)WillQoS << 3);
            if(hasUserName) flags |= 0b1000_0000;
            if(hasPassword) flags |= 0b0100_0000;
            if(WillRetain) flags |= 0b0010_0000;
            if(hasWillTopic) flags |= 0b0000_0100;
            if(CleanSession) flags |= 0b0000_0010;
            mem[0] = flags;
            mem = mem.Slice(1);

            // KeepAlive bytes
            WriteUInt16BigEndian(mem, KeepAlive);
            mem = mem.Slice(2);

            // Payload bytes
            if(hasClientId)
            {
                mem = mem.Slice(EncodeString(ClientId, mem));
            }
            else
            {
                mem[0] = 0;
                mem[1] = 0;
                mem = mem.Slice(2);
            }

            if(hasWillTopic)
            {
                mem = mem.Slice(EncodeString(WillTopic, mem));

                var messageSpan = WillMessage.Span;
                var spanLength = messageSpan.Length;
                WriteUInt16BigEndian(mem, (ushort)spanLength);
                mem = mem.Slice(2);
                messageSpan.CopyTo(mem);
                mem = mem.Slice(spanLength);
            }

            if(hasUserName) mem = mem.Slice(EncodeString(UserName, mem));

            if(hasPassword) EncodeString(Password, mem);

            return buffer;
        }

        internal int GetPayloadSize()
        {
            var willMessageLength = 2 + WillMessage.Length;

            return (IsNullOrEmpty(ClientId) ? 2 : 2 + UTF8.GetByteCount(ClientId)) +
                   (IsNullOrEmpty(UserName) ? 0 : 2 + UTF8.GetByteCount(UserName)) +
                   (IsNullOrEmpty(Password) ? 0 : 2 + UTF8.GetByteCount(Password)) +
                   (IsNullOrEmpty(WillTopic) ? 0 : 2 + UTF8.GetByteCount(WillTopic) + willMessageLength);
        }

        internal int GetHeaderSize()
        {
            return 6 + UTF8.GetByteCount(ProtocolName);
        }

        public static bool TryParse(ReadOnlySequence<byte> source, out ConnectPacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            packet = null;

            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               (PacketType)(flags & TypeMask) == Connect && offset + length <= source.Length)
            {
                packet = new ConnectPacket(null);

                source = source.Slice(offset);

                if(!TryReadString(source, out var protocol, out var consumed)) return false;
                packet.ProtocolName = protocol;
                source = source.Slice(consumed);

                if(!TryReadByte(source, out var level)) return false;

                packet.ProtocolLevel = level;

                source = source.Slice(1);

                if(!TryReadByte(source, out var connFlags)) return false;

                packet.WillQoS = (QoSLevel)((connFlags >> 3) & QoSMask);
                packet.WillRetain = (connFlags & 0b0010_0000) == 0b0010_0000;
                packet.CleanSession = (connFlags & 0b0000_0010) == 0b0000_0010;

                source = source.Slice(1);

                if(!TryReadUInt16(source, out var keepAlive)) return false;

                packet.KeepAlive = keepAlive;

                source = source.Slice(2);

                if(!TryReadString(source, out var id, out consumed)) return false;
                packet.ClientId = id;
                source = source.Slice(consumed);

                if((connFlags & 0b0000_0100) == 0b0000_0100)
                {
                    if(!TryReadString(source, out var topic, out consumed) || consumed <= 2) return false;
                    packet.WillTopic = topic;
                    source = source.Slice(consumed);

                    if(!TryReadUInt16(source, out var len)) return false;

                    if(len > 0)
                    {
                        packet.WillMessage = new byte[len];
                        source.Slice(2, len).CopyTo(packet.WillMessage.Span);
                    }

                    source = source.Slice(len + 2);
                }

                if((connFlags & 0b1000_0000) == 0b1000_0000)
                {
                    if(!TryReadString(source, out var userName, out consumed) || consumed <= 2) return false;
                    packet.UserName = userName;
                    source = source.Slice(consumed);
                }

                if((connFlags & 0b0100_0000) == 0b0100_0000)
                {
                    if(!TryReadString(source, out var password, out consumed) || consumed <= 2) return false;
                    packet.Password = password;
                }

                return true;
            }

            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out ConnectPacket packet)
        {
            if(TryParseHeader(source, out var flags, out var length, out var offset) &&
               (PacketType)(flags & TypeMask) == Connect && offset + length <= source.Length)
            {
                packet = new ConnectPacket(null);

                source = source.Slice(offset);

                var len = ReadUInt16BigEndian(source);

                packet.ProtocolName = UTF8.GetString(source.Slice(2, len));

                source = source.Slice(len + 2);

                packet.ProtocolLevel = source[0];

                source = source.Slice(1);

                var connFlags = source[0];

                packet.WillQoS = (QoSLevel)((connFlags >> 3) & QoSMask);
                packet.WillRetain = (connFlags & 0b0010_0000) == 0b0010_0000;
                packet.CleanSession = (connFlags & 0b0000_0010) == 0b0000_0010;

                source = source.Slice(1);

                packet.KeepAlive = ReadUInt16BigEndian(source);

                source = source.Slice(2);

                len = ReadUInt16BigEndian(source);

                if(len > 0) packet.ClientId = UTF8.GetString(source.Slice(2, len));

                source = source.Slice(len + 2);

                if((connFlags & 0b0000_0100) == 0b0000_0100)
                {
                    len = ReadUInt16BigEndian(source);

                    if(len == 0)
                    {
                        packet = null;
                        return false;
                    }

                    packet.WillTopic = UTF8.GetString(source.Slice(2, len));

                    source = source.Slice(len + 2);

                    len = ReadUInt16BigEndian(source);

                    if(len > 0)
                    {
                        packet.WillMessage = new byte[len];
                        source.Slice(2, len).CopyTo(packet.WillMessage.Span);
                    }

                    source = source.Slice(len + 2);
                }

                if((connFlags & 0b1000_0000) == 0b1000_0000)
                {
                    len = ReadUInt16BigEndian(source);
                    packet.UserName = UTF8.GetString(source.Slice(2, len));
                    source = source.Slice(len + 2);
                }

                if((connFlags & 0b0100_0000) == 0b0100_0000)
                {
                    len = ReadUInt16BigEndian(source);
                    packet.Password = UTF8.GetString(source.Slice(2, len));
                }

                return true;
            }

            packet = null;
            return false;
        }
    }
}