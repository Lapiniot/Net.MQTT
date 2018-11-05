using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        private async Task DispatchMessageAsync(object arg, CancellationToken cancellationToken)
        {
            var (topic, payload, qoSLevel, _) = await state.DequeueAsync(cancellationToken).ConfigureAwait(false);

            switch(qoSLevel)
            {
                case AtMostOnce:
                {
                    var t = SendPacketAsync(new PublishPacket(0, default, topic, payload), cancellationToken);
                    var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);
                    break;
                }
                case AtLeastOnce:
                case ExactlyOnce:
                {
                    var packet = state.AddResendPacket(id => new PublishPacket(id, qoSLevel, topic, payload));
                    var t = SendPacketAsync(packet, cancellationToken);
                    var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(qoSLevel), qoSLevel, null);
            }
        }

        protected override ValueTask<int> OnPubAckAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(!PubAckPacket.TryParse(buffer, out var id)) return new ValueTask<int>(0);

            state.RemoveResendPacket(id);

            return new ValueTask<int>(4);
        }

        protected override async ValueTask<int> OnPubRecAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(!PubRecPacket.TryParse(buffer, out var id)) return 0;

            var pubRelPacket = new PubRelPacket(id);
            state.UpdateResendPacket(id, pubRelPacket);
            var t = SendPublishResponseAsync(PubRel, id, cancellationToken);
            var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);

            return 4;
        }

        protected override ValueTask<int> OnPubCompAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(!PubCompPacket.TryParse(buffer, out var id)) return new ValueTask<int>(0);

            state.RemoveResendPacket(id);

            return new ValueTask<int>(4);
        }
    }
}