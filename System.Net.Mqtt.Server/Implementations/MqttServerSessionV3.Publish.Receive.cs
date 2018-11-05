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
        protected override async ValueTask<int> OnPublishAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(PublishPacket.TryParse(buffer, out var packet, out var consumed))
            {
                var message = new Message(packet.Topic, packet.Payload, packet.QoSLevel, packet.Retain);

                switch(packet.QoSLevel)
                {
                    case AtMostOnce:
                    {
                        OnMessageReceived(message);
                        break;
                    }
                    case AtLeastOnce:
                    {
                        OnMessageReceived(message);
                        var t = SendPublishResponseAsync(PubAck, packet.Id, cancellationToken);
                        var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);
                        break;
                    }
                    case ExactlyOnce:
                    {
                        // This is to avoid message duplicates for QoS 2
                        if(state.TryAddQoS2(packet.Id))
                        {
                            OnMessageReceived(message);
                        }

                        var t = SendPublishResponseAsync(PubRec, packet.Id, cancellationToken);
                        var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);
                        break;
                    }
                }

                return consumed;
            }

            return 0;
        }

        protected override async ValueTask<int> OnPubRelAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(!PubRelPacket.TryParse(buffer, out var id)) return 0;

            state.RemoveQoS2(id);
            var t = SendPublishResponseAsync(PubComp, id, cancellationToken);
            var _ = t.IsCompleted ? t.Result : await t.ConfigureAwait(false);

            return 4;
        }
    }
}