namespace Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    protected sealed override async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        foreach (var (id, state) in state!.PublishState)
        {
            stoppingToken.ThrowIfCancellationRequested();
            ResendPublish(id, in state);
        }

        var reader = state!.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (topic, payload, qos, retain) = message;
                var flags = retain ? PacketFlags.Retain : 0u;

                switch (qos)
                {
                    case QoSLevel.QoS0:
                        Post((topic, payload, flags));
                        break;

                    case QoSLevel.QoS1:
                    case QoSLevel.QoS2:
                        await inflightSentinel!.WaitAsync(stoppingToken).ConfigureAwait(false);
                        flags |= (uint)qos << 1;
                        var id = state.CreateMessageDeliveryState(new(flags, topic, payload));
                        Post((topic, payload, ((uint)id << 8) | flags));
                        break;

                    default:
                        reader.TryRead(out _);
                        InvalidQoSException.Throw();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }

    private void ResendPublish(ushort id, ref readonly PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
        {
            Post((state.Topic, state.Payload, ((uint)id << 8) | state.Flags | PacketFlags.Duplicate));
        }
        else
        {
            Post(PacketFlags.PubRelPacketMask | id);
        }
    }
}