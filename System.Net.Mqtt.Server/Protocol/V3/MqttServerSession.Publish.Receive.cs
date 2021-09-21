using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Packets.PublishPacket;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    protected override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadPayload(header, (int)reminder.Length, reminder, out var packet))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
        }

        var message = new Message(packet.Topic, packet.Payload, packet.QoSLevel, packet.Retain);

        switch(packet.QoSLevel)
        {
            case 0:
                OnMessageReceived(message);
                break;

            case 1:
                OnMessageReceived(message);
                Post(new PubAckPacket(packet.Id));
                break;

            case 2:
                // This is to avoid message duplicates for QoS 2
                if(sessionState.TryAddQoS2(packet.Id))
                {
                    OnMessageReceived(message);
                }

                Post(new PubRecPacket(packet.Id));
                break;

            default: throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
        }
    }

    protected override void OnPubRel(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!reminder.TryReadUInt16(out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBREL"));
        }

        sessionState.RemoveQoS2(id);
        Post(new PubCompPacket(id));
    }
}