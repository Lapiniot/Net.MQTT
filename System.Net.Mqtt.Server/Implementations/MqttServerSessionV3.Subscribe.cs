using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override async ValueTask<int> OnSubscribeAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(!SubscribePacket.TryParse(buffer, out var packet, out var consumed)) return 0;

            var subAckPacket = new SubAckPacket(packet.Id, state.Subscribe(packet.Topics));
            var t = SendPacketAsync(subAckPacket, cancellationToken);
            var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);

            return consumed;
        }

        protected override async ValueTask<int> OnUnsubscribeAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(!UnsubscribePacket.TryParse(buffer, out var packet, out var consumed)) return 0;

            state.Unsubscribe(packet.Topics);

            var id = packet.Id;
            var t = SendPacketAsync(new byte[] {(byte)UnsubAck, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
            var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);

            return consumed;
        }
    }
}