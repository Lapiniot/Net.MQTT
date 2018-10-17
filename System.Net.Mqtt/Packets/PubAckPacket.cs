using System.Buffers;

namespace System.Net.Mqtt.Packets
{
    public sealed class PubAckPacket : MqttPacketWithId
    {
        public PubAckPacket(ushort id) : base(id)
        {
        }

        protected override byte Header { get; } = (byte)PacketType.PubAck;

        public static bool TryParse(in ReadOnlySequence<byte> source, out PubAckPacket packet)
        {
            if(source.IsSingleSegment)
            {
                return TryParse(source.First.Span, out packet);
            }

            throw new NotImplementedException();
        }

        public static bool TryParse(in ReadOnlySpan<byte> source, out PubAckPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}