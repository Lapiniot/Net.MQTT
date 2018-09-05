using System.Buffers.Binary;
using System.Text;

namespace System.Net.Mqtt.Packets
{
    public sealed class ConnectPacket : MqttPacket
    {
        public ConnectPacket(string clientId)
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

            var buffer = new byte[1 + MqttHelpers.GetLengthByteCount(length) + length];

            var mem = (Span<byte>)buffer;

            // Packet flags
            mem[0] = (byte)PacketType.Connect;
            mem = mem.Slice(1);

            // Remaining length bytes
            mem = mem.Slice(MqttHelpers.EncodeLengthBytes(length, mem));

            // Protocol info bytes
            mem = mem.Slice(MqttHelpers.EncodeString(ProtocolName, mem));
            mem[0] = ProtocolLevel;
            mem = mem.Slice(1);

            // Connection flag
            var flags = (byte)((byte)WillQoS << 3);
            if(!string.IsNullOrEmpty(UserName)) flags |= 0b1000_0000;
            if(!string.IsNullOrEmpty(Password)) flags |= 0b0100_0000;
            if(WillRetain) flags |= 0b0010_0000;
            if(WillMessage.Length > 0) flags |= 0b0000_0100;
            if(CleanSession) flags |= 0b0000_0010;
            mem[0] = flags;
            mem = mem.Slice(1);

            // KeepAlive bytes
            BinaryPrimitives.WriteInt16BigEndian(mem, KeepAlive);
            mem = mem.Slice(2);

            // Payload bytes
            if(!string.IsNullOrEmpty(ClientId)) mem = mem.Slice(MqttHelpers.EncodeString(ClientId, mem));
            if(!string.IsNullOrEmpty(WillTopic)) mem = mem.Slice(MqttHelpers.EncodeString(WillTopic, mem));
            if(WillMessage.Length > 0)
            {
                var messageSpan = WillMessage.Span;
                var spanLength = messageSpan.Length;
                BinaryPrimitives.WriteUInt16BigEndian(mem, (ushort)spanLength);
                mem = mem.Slice(2);
                messageSpan.CopyTo(mem);
                mem = mem.Slice(spanLength);
            }

            if(!string.IsNullOrEmpty(UserName)) mem = mem.Slice(MqttHelpers.EncodeString(UserName, mem));
            if(!string.IsNullOrEmpty(Password)) MqttHelpers.EncodeString(Password, mem);

            return buffer;
        }

        internal int GetPayloadSize()
        {
            var willMessageLength = WillMessage.Length;

            return (string.IsNullOrEmpty(ClientId) ? 0 : 2 + Encoding.UTF8.GetByteCount(ClientId)) +
                   (string.IsNullOrEmpty(UserName) ? 0 : 2 + Encoding.UTF8.GetByteCount(UserName)) +
                   (string.IsNullOrEmpty(Password) ? 0 : 2 + Encoding.UTF8.GetByteCount(Password)) +
                   (string.IsNullOrEmpty(WillTopic) ? 0 : 2 + Encoding.UTF8.GetByteCount(WillTopic)) +
                   (willMessageLength > 0 ? 2 + willMessageLength : 0);
        }

        internal int GetHeaderSize()
        {
            return 6 + Encoding.UTF8.GetByteCount(ProtocolName);
        }
    }
}