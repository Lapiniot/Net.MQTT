namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    protected sealed override async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        foreach (var (id, state) in state!.PublishState)
        {
            if (stoppingToken.IsCancellationRequested) break;
            ResendPublish(id, in state);
        }

        var reader = state!.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (topic, payload, qos, retain) = message;
                var flags = retain ? PacketFlags.Retain : 0;

                switch (qos)
                {
                    case QoSLevel.QoS0:
                        PostPublish((byte)flags, 0, topic, in payload);
                        break;

                    case QoSLevel.QoS1:
                    case QoSLevel.QoS2:
                        await inflightSentinel!.WaitAsync(stoppingToken).ConfigureAwait(false);
                        flags |= (int)qos << 1;
                        var id = state.CreateMessageDeliveryState(new(flags, topic, payload));
                        PostPublish((byte)flags, id, topic, in payload);
                        break;

                    default:
                        InvalidQoSException.Throw();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }

    private void ResendPublish(ushort id, in PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
            PostPublish((byte)(state.Flags | PacketFlags.Duplicate), id, state.Topic, state.Payload);
        else
            Post(PacketFlags.PubRelPacketMask | id);
    }
}