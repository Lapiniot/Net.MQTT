using Net.Mqtt.Packets.V3;
using static Net.Mqtt.PacketFlags;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >>> 1) & QoSMask;
        if (!PublishPacket.TryReadPayloadExact(in reminder, (int)reminder.Length, readPacketId: qos != 0, out var id, out var topic, out var payload))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        var retain = (header & Retain) == Retain;

        switch (qos)
        {
            case 0:
                DispatchMessage(topic, payload, retain);
                break;

            case 1:
                DispatchMessage(topic, payload, retain);
                Post(PubAckPacketMask | id);
                break;

            case 2:
                if (sessionState!.TryAddQoS2(id))
                {
                    DispatchMessage(topic, payload, retain);
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MalformedPacketException.Throw("PUBLISH");
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        sessionState!.RemoveQoS2(id);

        Post(PubCompPacketMask | id);
    }

    private void DispatchMessage(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, bool retained)
    {
        var message = new MqttMessage(topic, payload, retained);
        OnMessageReceived(ref message);
    }
}