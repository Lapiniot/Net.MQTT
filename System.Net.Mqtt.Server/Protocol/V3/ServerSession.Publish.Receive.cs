using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Properties;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        protected override async Task OnPublishAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if((header & 0b11_0000) != 0b11_0000 || !PublishPacket.TryParsePayload(header, buffer, out var packet))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "PUBLISH"));
            }

            var message = new Message(packet.Topic, packet.Payload, packet.QoSLevel, packet.Retain);

            switch(packet.QoSLevel)
            {
                case QoSLevel.AtMostOnce:
                {
                    OnMessageReceived(message);
                    break;
                }
                case QoSLevel.AtLeastOnce:
                {
                    OnMessageReceived(message);
                    await SendPublishResponseAsync(PacketType.PubAck, packet.Id, cancellationToken).ConfigureAwait(false);
                    break;
                }
                case QoSLevel.ExactlyOnce:
                {
                    // This is to avoid message duplicates for QoS 2
                    if(state.TryAddQoS2(packet.Id))
                    {
                        OnMessageReceived(message);
                    }

                    await SendPublishResponseAsync(PacketType.PubRec, packet.Id, cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
        }

        protected override async Task OnPubRelAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0110_0000 || !MqttHelpers.TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "PUBREL"));
            }

            state.RemoveQoS2(id);

            await SendPublishResponseAsync(PacketType.PubComp, id, cancellationToken).ConfigureAwait(false);
        }
    }
}