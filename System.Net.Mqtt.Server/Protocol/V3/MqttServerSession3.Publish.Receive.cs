using System.Net.Mqtt.Packets.V3;
using static System.Net.Mqtt.PacketFlags;
using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >> 1) & QoSMask;
        if (!PublishPacket.TryReadPayload(in reminder, qos != 0, (int)reminder.Length, out var id, out var topic, out var payload))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
        }

        var message = new Message(topic, payload, (byte)qos, (header & Retain) == Retain);

        switch (qos)
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
                if (sessionState!.TryAddQoS2(id))
                {
                    OnMessageReceived(message);
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREL");
        }

        sessionState!.RemoveQoS2(id);
        Post(PubCompPacketMask | id);
    }
}