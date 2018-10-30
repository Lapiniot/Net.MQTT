using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketType;
using static System.String;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Packets
{
    public abstract class ConnectPacket : MqttPacket
    {
        protected ConnectPacket(string clientId, byte protocolLevel, string protocolName,
            ushort keepAlive, bool cleanSession, string userName, string password,
            string willTopic, Memory<byte> willMessage, QoSLevel willQoS, bool willRetain)
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
        public QoSLevel WillQoS { get; }
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
    }
}