using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    protected sealed override async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
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
                    case 0:
                        Post(new PublishPacket(0, 0, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            Properties = message.Properties,
                            MessageExpiryInterval = expiryInterval
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
                            Properties = message.Properties,
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    private void ResendPublish(ushort id, Message5 message)
    {
        if (!message.Topic.IsEmpty)
        {
            uint? expiryInterval = null;
            if (message.ExpiresAt is { } expiresAt && !IsNotExpired(expiresAt, out expiryInterval))
            {
                // Discard expired message
                return;
            }

            Post(new PublishPacket(id, message.QoSLevel, message.Topic, message.Payload, message.Retain)
            {
                SubscriptionIds = message.SubscriptionIds,
                ContentType = message.ContentType,
                PayloadFormat = message.PayloadFormat,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                Properties = message.Properties,
                MessageExpiryInterval = expiryInterval
            });
        }
        else
        {
            Post(PacketFlags.PubRelPacketMask | id);
        }
    }
}
