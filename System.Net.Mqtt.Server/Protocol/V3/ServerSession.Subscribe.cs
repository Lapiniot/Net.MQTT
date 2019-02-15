using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        protected override void OnSubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10000010 || !SubscribePacket.TryReadPayload(buffer, (int)buffer.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "SUBSCRIBE"));
            }

            var result = state.Subscribe(packet.Topics);

            Post(new SubAckPacket(packet.Id, result).GetBytes());

            Server.OnSubscribe(state, packet.Topics);
        }

        protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10100010 || !UnsubscribePacket.TryReadPayload(buffer, (int)buffer.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "UNSUBSCRIBE"));
            }

            state.Unsubscribe(packet.Topics);

            var id = packet.Id;

            Post(new byte[] {(byte)PacketType.UnsubAck, 2, (byte)(id >> 8), (byte)id});
        }
    }
}