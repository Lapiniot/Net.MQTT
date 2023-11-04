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
                var flags = retain ? PacketFlags.Retain : (byte)0;

                switch (qos)
                {
                    case 0:
                        PostPublish(flags, 0, topic, in payload);
                        break;

                    case 1:
                    case 2:
                        await inflightSentinel!.WaitAsync(stoppingToken).ConfigureAwait(false);
                        flags |= (byte)(qos << 1);
                        var id = state.CreateMessageDeliveryState(new(flags, topic, payload));
                        PostPublish(flags, id, topic, in payload);
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