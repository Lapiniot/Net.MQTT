using System.Buffers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubAckPacket : MqttPacketWithId
    {
        public PubAckPacket(ushort id) : base(id) {}

        protected override byte Header { get; } = (byte)PubAck;

        public static bool TryParse(in ReadOnlySequence<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubAck, out id);
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubAck, out id);
        }
    }
}