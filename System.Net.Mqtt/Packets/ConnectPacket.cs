using System.Buffers;
using System.Net.Mqtt.Extensions;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketType;
using static System.String;
using static System.Text.Encoding;
using SequenceReaderExtensions = System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets
{
    public class ConnectPacket : MqttPacket
    {
        public ConnectPacket(string clientId, byte protocolLevel, string protocolName,
            ushort keepAlive = 120, bool cleanSession = true, string userName = null, string password = null,
            string willTopic = null, Memory<byte> willMessage = default, byte willQoS = 0x00, bool willRetain = false)
        {
            ClientId = clientId;
            ProtocolLevel = protocolLevel;
            ProtocolName = protocolName;
            KeepAlive = keepAlive;
            CleanSession = cleanSession;
            UserName = userName;
            Password = password;
            WillTopic = willTopic;
            WillMessage = willMessage;
            WillQoS = willQoS;
            WillRetain = willRetain;
        }

        public ushort KeepAlive { get; }
        public string UserName { get; }
        public string Password { get; }
        public string ClientId { get; }
        public string WillTopic { get; }
        public Memory<byte> WillMessage { get; }
        public byte WillQoS { get; }
        public bool WillRetain { get; }
        public bool CleanSession { get; }
        public string ProtocolName { get; }
        public byte ProtocolLevel { get; }

        public override Memory<byte> GetBytes()
        {
            var hasClientId = !IsNullOrEmpty(ClientId);
            var hasUserName = !IsNullOrEmpty(UserName);
            var hasPassword = !IsNullOrEmpty(Password);
            var hasWillTopic = !IsNullOrEmpty(WillTopic);

            var length = GetHeaderSize() + GetPayloadSize();

            var buffer = new byte[1 + SpanExtensions.GetLengthByteCount(length) + length];

            var mem = (Span<byte>)buffer;

            // Packet flags
            mem[0] = (byte)Connect;
            mem = mem.Slice(1);

            // Remaining length bytes
            mem = mem.Slice(SpanExtensions.EncodeMqttLengthBytes(length, mem));

            // Protocol info bytes
            mem = mem.Slice(SpanExtensions.EncodeMqttString(ProtocolName, mem));
            mem[0] = ProtocolLevel;
            mem = mem.Slice(1);


            // Connection flag
            var flags = (byte)(WillQoS << 3);
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
                mem = mem.Slice(SpanExtensions.EncodeMqttString(ClientId, mem));
            }
            else
            {
                mem[0] = 0;
                mem[1] = 0;
                mem = mem.Slice(2);
            }

            if(hasWillTopic)
            {
                mem = mem.Slice(SpanExtensions.EncodeMqttString(WillTopic, mem));

                var messageSpan = WillMessage.Span;
                var spanLength = messageSpan.Length;
                WriteUInt16BigEndian(mem, (ushort)spanLength);
                mem = mem.Slice(2);
                messageSpan.CopyTo(mem);
                mem = mem.Slice(spanLength);
            }

            if(hasUserName) mem = mem.Slice(SpanExtensions.EncodeMqttString(UserName, mem));

            if(hasPassword) SpanExtensions.EncodeMqttString(Password, mem);

            return buffer;
        }

        protected internal int GetPayloadSize()
        {
            var willMessageLength = 2 + WillMessage.Length;

            return (IsNullOrEmpty(ClientId) ? 2 : 2 + UTF8.GetByteCount(ClientId)) +
                   (IsNullOrEmpty(UserName) ? 0 : 2 + UTF8.GetByteCount(UserName)) +
                   (IsNullOrEmpty(Password) ? 0 : 2 + UTF8.GetByteCount(Password)) +
                   (IsNullOrEmpty(WillTopic) ? 0 : 2 + UTF8.GetByteCount(WillTopic) + willMessageLength);
        }

        protected internal int GetHeaderSize()
        {
            return 6 + UTF8.GetByteCount(ProtocolName);
        }

        public static bool TryParse(ReadOnlySequence<byte> sequence, out ConnectPacket packet, out int consumed)
        {
            // Fast path
            if(sequence.IsSingleSegment) return TryParse(sequence.First.Span, out packet, out consumed);

            packet = null;
            consumed = 0;

            var sr = new SequenceReader<byte>(sequence);

            if(!SequenceReaderExtensions.TryReadMqttHeader(ref sr, out var header, out var length) || length > sr.Remaining) return false;

            if(header != 0b0001_0000 || !TryReadPayload(ref sr, out packet)) return false;

            consumed = (int)sr.Consumed;

            return true;
        }

        public static bool TryReadPayload(ref SequenceReader<byte> reader, out ConnectPacket packet)
        {
            if(reader.Sequence.IsSingleSegment) return TryParsePayload(reader.UnreadSpan, out packet);

            packet = null;

            if(!reader.TryReadMqttString(out var protocol)) return false;
            if(!reader.TryRead(out var level)) return false;
            if(!reader.TryRead(out var connFlags)) return false;
            if(!reader.TryReadBigEndian(out ushort keepAlive)) return false;
            if(!reader.TryReadMqttString(out var clientId)) return false;

            string topic = null;
            byte[] willMessage = null;
            if((connFlags & 0b0000_0100) == 0b0000_0100)
            {
                if(!reader.TryReadMqttString(out topic)) return false;
                if(!reader.TryReadBigEndian(out ushort size)) return false;

                if(size > 0)
                {
                    willMessage = new byte[size];
                    if(!reader.TryCopyTo(willMessage)) return false;
                    reader.Advance(size);
                }
            }

            string userName = null;
            if((connFlags & 0b1000_0000) == 0b1000_0000 && !reader.TryReadMqttString(out userName)) return false;

            string password = null;
            if((connFlags & 0b0100_0000) == 0b0100_0000 && !reader.TryReadMqttString(out password)) return false;

            packet = new ConnectPacket(clientId, level, protocol, keepAlive,
                (connFlags & 0b0010) == 0b0010, userName, password, topic, willMessage,
                (byte)((connFlags >> 3) & PacketFlags.QoSMask), (connFlags & 0b0010_0000) == 0b0010_0000);

            return true;
        }


        public static bool TryParse(ReadOnlySpan<byte> source, out ConnectPacket packet, out int consumed)
        {
            packet = null;
            consumed = 0;

            if(!SpanExtensions.TryReadMqttHeader(source, out var flags, out var length, out var offset) || offset + length > source.Length) return false;

            if(flags != 0b0001_0000 || !TryParsePayload(source.Slice(offset, length), out packet)) return false;

            consumed = offset + length;
            return true;
        }

        public static bool TryParsePayload(ReadOnlySequence<byte> source, out ConnectPacket packet)
        {
            if(source.IsSingleSegment) return TryParsePayload(source.First.Span, out packet);

            var sr = new SequenceReader<byte>(source);
            return TryReadPayload(ref sr, out packet);
        }

        public static bool TryParsePayload(ReadOnlySpan<byte> payload, out ConnectPacket packet)
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

            packet = new ConnectPacket(clientId, level, protocol, keepAlive,
                (connFlags & 0x2) == 0x2, userName, password, willTopic, willMessage,
                (byte)((connFlags >> 3) & PacketFlags.QoSMask), (connFlags & 0x2_0) == 0x2_0);

            return true;
        }
    }
}