using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Packets.PublishPacket;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    protected sealed override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadPayload(in reminder, header, (int)reminder.Length, out var id, out var topic, out var payload))
        {
            ThrowInvalidPacketFormat("PUBLISH");
        }

        var qosLevel = (byte)((header >> 1) & QoSMask);
        var message = new Message(topic, payload, qosLevel, (header & Retain) == Retain);

        switch (qosLevel)
        {
            case 0:
                OnMessageReceived(message);
                break;

            case 1:
                OnMessageReceived(message);
                Post(PubAckPacketMask | id);
                break;

            case 2:
                // This is to avoid message duplicates for QoS 2
                if (sessionState.TryAddQoS2(id))
                {
                    OnMessageReceived(message);
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                ThrowInvalidPacketFormat("PUBLISH");
                break;
        }
    }

    protected sealed override void OnPubRel(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBREL");
        }

        sessionState.RemoveQoS2(id);
        Post(PubCompPacketMask | id);
    }
}