using System.Buffers;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubCompPacket : MqttPacketWithId
    {
        public PubCompPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PacketType.PubComp;

        public static bool TryParse(in ReadOnlySequence<byte> source, out PubCompPacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            throw new NotImplementedException();
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out PubCompPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}