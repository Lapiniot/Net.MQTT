using static System.String;
using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Net.Mqtt.Messages
{
    public class ConnectMessage : MqttMessage
    {
        public ConnectMessage(string clientId)
        {
            ClientId = clientId;
        }

        public short KeepAlive { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public string LastWillTopic { get; set; }
        public string LastWillMessage { get; set; }
        public QoSLevel LastWillQoS { get; set; }
        public bool LastWillRetain { get; set; }
        public bool CleanSession { get; set; } = true;
        public string ProtocolName { get; set; } = "MQIsdp";
        public byte ProtocolVersion { get; set; } = 0x03;

        public override Memory<byte> GetBytes()
        {
            var length = GetHeaderSize() + GetPayloadSize();

            var buffer = new byte[1 + GetLengthByteCount(length) + length];

            var mem = (Span<byte>)buffer;

            mem[0] = (byte)PacketType.Connect;
            mem = mem.Slice(1);

            mem = mem.Slice(EncodeLengthBytes(length, mem));

            mem = mem.Slice(EncodeString(ProtocolName, mem));
            mem[0] = ProtocolVersion;
            mem = mem.Slice(1);

            var flags = (byte)((byte)LastWillQoS << 3);
            if(!IsNullOrEmpty(UserName)) flags |= 0b1000_0000;
            if(!IsNullOrEmpty(Password)) flags |= 0b0100_0000;
            if(LastWillRetain) flags |= 0b0010_0000;
            if(!IsNullOrEmpty(LastWillMessage)) flags |= 0b0000_0100;
            if(CleanSession) flags |= 0b0000_0010;
            mem[0] = flags;
            mem = mem.Slice(1);

            WriteInt16BigEndian(mem, KeepAlive);
            mem = mem.Slice(2);

            mem = mem.Slice(EncodeString(ClientId, mem));
            if(!IsNullOrEmpty(LastWillTopic)) mem = mem.Slice(EncodeString(LastWillTopic, mem));
            if(!IsNullOrEmpty(LastWillMessage)) mem = mem.Slice(EncodeString(LastWillMessage, mem));
            if(!IsNullOrEmpty(UserName)) mem = mem.Slice(EncodeString(UserName, mem));
            if(!IsNullOrEmpty(Password)) EncodeString(Password, mem);

            return buffer;
        }

        private int GetPayloadSize()
        {
            return (IsNullOrEmpty(ClientId) ? 0 : 2 + UTF8.GetByteCount(ClientId)) +
                   (IsNullOrEmpty(UserName) ? 0 : 2 + UTF8.GetByteCount(UserName)) +
                   (IsNullOrEmpty(Password) ? 0 : 2 + UTF8.GetByteCount(Password)) +
                   (IsNullOrEmpty(LastWillTopic) ? 0 : 2 + UTF8.GetByteCount(LastWillTopic)) +
                   (IsNullOrEmpty(LastWillMessage) ? 0 : 2 + UTF8.GetByteCount(LastWillMessage));
        }

        private int GetHeaderSize()
        {
            return 6 + UTF8.GetByteCount(ProtocolName);
        }
    }
}