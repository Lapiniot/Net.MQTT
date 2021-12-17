using System.Buffers;
using static System.String;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Packets.PublishPacket;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    protected override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadPayload(in reminder, header, (int)reminder.Length, out var packet))
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
                Post(PubAckPacketMask | packet.Id);
                break;

            case 2:
                // This is to avoid message duplicates for QoS 2
                if(sessionState.TryAddQoS2(packet.Id))
                {
                    OnMessageReceived(message);
                }

                Post(PubRecPacketMask | packet.Id);
                break;

            default: throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
        }
    }

    protected override void OnPubRel(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadUInt16(in reminder, out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBREL"));
        }

        sessionState.RemoveQoS2(id);
        Post(PubCompPacketMask | id);
    }
}