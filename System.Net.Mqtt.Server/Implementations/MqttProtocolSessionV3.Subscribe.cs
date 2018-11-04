using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override bool OnSubscribe(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(SubscribePacket.TryParse(buffer, out var packet, out consumed))
            {
                SendSubAckAsync(packet.Id, state.Subscribe(packet.Topics));

                return true;
            }

            return false;
        }

        protected override bool OnUnsubscribe(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(UnsubscribePacket.TryParse(buffer, out var packet, out consumed))
            {
                state.Unsubscribe(packet.Topics);

                SendUnsubAckAsync(packet.Id);

                return true;
            }

            return false;
        }

        public ValueTask<int> SendSubAckAsync(ushort id, byte[] result, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new SubAckPacket(id, result), cancellationToken);
        }

        public ValueTask<int> SendUnsubAckAsync(ushort id, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new byte[] {(byte)UnsubAck, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
        }
    }
}