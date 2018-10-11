using System.Buffers;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubRecPacket : MqttPacketWithId
    {
        public PubRecPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PacketType.PubRec;

        public static bool TryParse(in ReadOnlySequence<byte> source, out PubRecPacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            throw new NotImplementedException();
        }

        private static bool TryParse(in ReadOnlySpan<byte> source, out PubRecPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}