using static System.String;
using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Messages
{
    public sealed class ConnectMessage : MqttMessage
    {
        public ConnectMessage(string clientId)
        {
            ClientId = clientId;
        }

        public short KeepAlive { get; set; }
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
            var length = GetHeaderSize() + GetPayloadSize();

            var buffer = new byte[1 + GetLengthByteCount(length) + length];

            var mem = (Span<byte>)buffer;

            // Packet flags
            mem[0] = (byte)PacketType.Connect;
            mem = mem.Slice(1);

            // Remaining length bytes
            mem = mem.Slice(EncodeLengthBytes(length, mem));

            // Protocol info bytes
            mem = mem.Slice(EncodeString(ProtocolName, mem));
            mem[0] = ProtocolLevel;
            mem = mem.Slice(1);

            // Connection flag
            var flags = (byte)((byte)WillQoS << 3);
            if(!IsNullOrEmpty(UserName)) flags |= 0b1000_0000;
            if(!IsNullOrEmpty(Password)) flags |= 0b0100_0000;
            if(WillRetain) flags |= 0b0010_0000;
            if(WillMessage.Length > 0) flags |= 0b0000_0100;
            if(CleanSession) flags |= 0b0000_0010;
            mem[0] = flags;
            mem = mem.Slice(1);

            // KeepAlive bytes
            WriteInt16BigEndian(mem, KeepAlive);
            mem = mem.Slice(2);

            // Payload bytes
            if(!IsNullOrEmpty(ClientId)) mem = mem.Slice(EncodeString(ClientId, mem));
            if(!IsNullOrEmpty(WillTopic)) mem = mem.Slice(EncodeString(WillTopic, mem));
            if(WillMessage.Length > 0)
            {
                var messageSpan = WillMessage.Span;
                var spanLength = messageSpan.Length;
                WriteUInt16BigEndian(mem, (ushort)spanLength);
                mem = mem.Slice(2);
                messageSpan.CopyTo(mem);
                mem = mem.Slice(spanLength);
            }

            if(!IsNullOrEmpty(UserName)) mem = mem.Slice(EncodeString(UserName, mem));
            if(!IsNullOrEmpty(Password)) EncodeString(Password, mem);

            return buffer;
        }

        internal int GetPayloadSize()
        {
            var willMessageLength = WillMessage.Length;

            return (IsNullOrEmpty(ClientId) ? 0 : 2 + UTF8.GetByteCount(ClientId)) +
                   (IsNullOrEmpty(UserName) ? 0 : 2 + UTF8.GetByteCount(UserName)) +
                   (IsNullOrEmpty(Password) ? 0 : 2 + UTF8.GetByteCount(Password)) +
                   (IsNullOrEmpty(WillTopic) ? 0 : 2 + UTF8.GetByteCount(WillTopic)) +
                   (willMessageLength > 0 ? 2 + willMessageLength : 0);
        }

        internal int GetHeaderSize()
        {
            return 6 + UTF8.GetByteCount(ProtocolName);
        }
    }
}