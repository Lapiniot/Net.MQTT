namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        var reader = sessionState.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (topic, payload, qos, _) = message;

                switch (qos)
                {
                    case 0:
                        PostPublish(0, 0, topic, in payload);
                        break;

                    case 1:
                    case 2:
                        var flags = (byte)(qos << 1);
                        var id = await sessionState.CreateMessageDeliveryStateAsync(flags, topic, payload, stoppingToken).ConfigureAwait(false);
                        PostPublish(flags, id, topic, in payload);
                        break;

                    default:
                        ThrowInvalidQoS();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }

    protected sealed override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBACK");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }

    protected sealed override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);
        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected sealed override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBCOMP");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }

    [DoesNotReturn]
    private static void ThrowInvalidQoS() => throw new InvalidDataException("Invalid QoS level value.");
}