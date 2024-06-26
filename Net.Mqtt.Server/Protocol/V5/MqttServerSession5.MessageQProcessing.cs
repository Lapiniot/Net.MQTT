using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    private readonly AsyncSemaphoreLight inflightSentinel;

    protected sealed override async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        foreach (var (id, message) in state!.PublishState)
        {
            if (stoppingToken.IsCancellationRequested) break;
            ResendPublish(id, message);
        }

        var reader = state!.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                uint? expiryInterval = null;
                if (message.ExpiresAt is { } expiresAt && !IsNotExpired(expiresAt, out expiryInterval))
                {
                    // Discard expired message
                    reader.TryRead(out _);
                    continue;
                }

                var (topic, payload, qos, retain) = message;

                switch (qos)
                {
                    case QoSLevel.QoS0:
                        Post(new PublishPacket(0, 0, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            UserProperties = message.UserProperties,
                            MessageExpiryInterval = expiryInterval
                        });
                        break;

                    case QoSLevel.QoS1:
                    case QoSLevel.QoS2:
                        await inflightSentinel.WaitAsync(stoppingToken).ConfigureAwait(false);
                        var id = state.CreateMessageDeliveryState(in message);
                        Post(new PublishPacket(id, qos, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            UserProperties = message.UserProperties,
                            MessageExpiryInterval = expiryInterval
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

    private static bool IsNotExpired(long expiresAtUtcTicks, out uint? expiryIntervalSeconds)
    {
        var now = DateTime.UtcNow.Ticks;
        if (expiresAtUtcTicks > now)
        {
            expiryIntervalSeconds = (uint)Math.Ceiling((double)(expiresAtUtcTicks - now) / TimeSpan.TicksPerSecond);
            return true;
        }
        else
        {
            expiryIntervalSeconds = null;
            return false;
        }
    }

    private void ResendPublish(ushort id, Message5? message)
    {
        if (message is not null)
        {
            Post(new PublishPacket(id, message.QoSLevel, message.Topic, message.Payload, message.Retain, duplicate: true)
            {
                SubscriptionIds = message.SubscriptionIds,
                ContentType = message.ContentType,
                PayloadFormat = message.PayloadFormat,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                UserProperties = message.UserProperties
            });
        }
        else
        {
            Post(PacketFlags.PubRelPacketMask | id);
        }
    }
}
