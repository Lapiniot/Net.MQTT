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
        protected override bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PublishPacket.TryParse(buffer, out var packet, out consumed))
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
                        SendPubAckAsync(packet.Id);
                        break;
                    }
                    case ExactlyOnce:
                    {
                        // This is to avoid message duplicates for QoS 2
                        if(state.TryAddQoS2(packet.Id))
                        {
                            OnMessageReceived(message);
                        }

                        SendPubRecAsync(packet.Id);
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        protected override bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubRelPacket.TryParse(buffer, out var id))
            {
                state.RemoveQoS2(id);
                consumed = 4;
                SendPubCompAsync(id);
                return true;
            }

            consumed = 0;
            return false;
        }

        public ValueTask<int> SendPubAckAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubAck, id, cancellationToken);
        }

        public ValueTask<int> SendPubRecAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubRec, id, cancellationToken);
        }

        public ValueTask<int> SendPubCompAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubComp, id, cancellationToken);
        }
    }
}