using System.Buffers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubCompPacket : MqttPacketWithId
    {
        public PubCompPacket(ushort id) : base(id) {}

        protected override byte Header { get; } = (byte)PubComp;

        public static bool TryParse(in ReadOnlySequence<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubComp, out id);
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubComp, out id);
        }
    }
}