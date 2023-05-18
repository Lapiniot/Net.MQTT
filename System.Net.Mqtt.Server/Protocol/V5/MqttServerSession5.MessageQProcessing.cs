using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        var reader = state!.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (topic, payload, qos, retain) = message;

                switch (qos)
                {
                    case 0:
                        Post(new PublishPacket(0, 0, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            Properties = message.Properties
                        });
                        break;

                    case 1:
                    case 2:
                        var id = await state.CreateMessageDeliveryStateAsync(message, stoppingToken).ConfigureAwait(false);
                        Post(new PublishPacket(id, qos, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            Properties = message.Properties
                        });
                        break;

                    default:
                        InvalidQoSException.Throw();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }
}
