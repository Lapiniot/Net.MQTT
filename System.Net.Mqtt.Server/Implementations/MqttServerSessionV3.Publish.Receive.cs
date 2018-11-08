using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override async Task OnPublishAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if((header & 0b11_0000) != 0b11_0000 || !PublishPacket.TryParsePayload(header, buffer, out var packet))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBLISH"));
            }

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
                    await SendPublishResponseAsync(PubAck, packet.Id, cancellationToken).ConfigureAwait(false);
                    break;
                }
                case ExactlyOnce:
                {
                    // This is to avoid message duplicates for QoS 2
                    if(state.TryAddQoS2(packet.Id))
                    {
                        OnMessageReceived(message);
                    }

                    await SendPublishResponseAsync(PubRec, packet.Id, cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
        }

        protected override async Task OnPubRelAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0110_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBREL"));
            }

            state.RemoveQoS2(id);

            await SendPublishResponseAsync(PubComp, id, cancellationToken).ConfigureAwait(false);
        }
    }
}