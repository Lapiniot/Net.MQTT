using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Packets.PublishPacket;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        protected override Task OnPublishAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if((header & 0b11_0000) != 0b11_0000 || !TryParsePayload(header, buffer, out var packet))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBLISH"));
            }

            var message = new Message(packet.Topic, packet.Payload, packet.QoSLevel, packet.Retain);

            switch(packet.QoSLevel)
            {
                case 0:
                {
                    OnMessageReceived(message);
                    break;
                }
                case 1:
                {
                    OnMessageReceived(message);
                    PostPublishResponse(PubAck, packet.Id);
                    break;
                }
                case 2:
                {
                    // This is to avoid message duplicates for QoS 2
                    if(state.TryAddQoS2(packet.Id))
                    {
                        OnMessageReceived(message);
                    }

                    PostPublishResponse(PubRec, packet.Id);
                    break;
                }
            }

            return Task.CompletedTask;
        }

        protected override Task OnPubRelAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0110_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBREL"));
            }

            state.RemoveQoS2(id);

            PostPublishResponse(PubComp, id);

            return Task.CompletedTask;
        }
    }
}