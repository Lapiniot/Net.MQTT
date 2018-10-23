using System.Buffers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubRelPacket : MqttPacketWithId
    {
        public PubRelPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PubRel | 0b0010;

        public static bool TryParse(in ReadOnlySequence<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubRel, out id);
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubRel, out id);
        }
    }
}