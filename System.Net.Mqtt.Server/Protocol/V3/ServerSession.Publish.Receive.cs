using System.Buffers;
using System.IO;
using System.Net.Mqtt.Extensions;
using static System.Net.Mqtt.Packets.PublishPacket;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        protected override void OnPublish(byte header, ReadOnlySequence<byte> buffer)
        {
            if((header & 0b11_0000) != 0b11_0000 || !TryReadPayload(header, (int)buffer.Length, buffer, out var packet))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBLISH"));
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
                    PostPublishResponse(0b0100_0000, packet.Id);
                    break;
                }
                case 2:
                {
                    // This is to avoid message duplicates for QoS 2
                    if(state.TryAddQoS2(packet.Id))
                    {
                        OnMessageReceived(message);
                    }

                    PostPublishResponse(0b0101_0000, packet.Id);
                    break;
                }
            }
        }

        protected override void OnPubRel(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header >> 4 != 0b0110 || !buffer.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBREL"));
            }

            state.RemoveQoS2(id);

            PostPublishResponse(0b0111_0000, id);
        }
    }
}