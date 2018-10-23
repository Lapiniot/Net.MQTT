using System.Buffers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubRecPacket : MqttPacketWithId
    {
        public PubRecPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PubRec;

        public static bool TryParse(in ReadOnlySequence<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubRec, out id);
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out ushort id)
        {
            return TryParseGeneric(source, PubRec, out id);
        }
    }
}