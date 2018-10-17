using System.Buffers;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubRelPacket : MqttPacketWithId
    {
        public PubRelPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PacketType.PubRel | 0b0010;

        public static bool TryParse(in ReadOnlySequence<byte> source, out PubRelPacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            throw new NotImplementedException();
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out PubRelPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}