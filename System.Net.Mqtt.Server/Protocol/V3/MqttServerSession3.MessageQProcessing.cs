namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    protected sealed override async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
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
                        flags |= (byte)(qos << 1);
                        var id = await state.CreateMessageDeliveryStateAsync(flags, topic, payload, stoppingToken).ConfigureAwait(false);
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

    private void ResendPublish(ushort id, PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
            PostPublish(state.Flags, id, state.Topic, state.Payload);
        else
            Post(PacketFlags.PubRelPacketMask | id);
    }
}