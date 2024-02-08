using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;

namespace System.Net.Mqtt.Server;

public sealed class RetainedMessageHandler5 : RetainedMessageStore<Message5>
{
    public void OnNext([NotNull] MqttServerSessionState3 state, (byte[] Filter, byte QoS) subscription)
    {
        var writer = state.OutgoingWriter;
        ReadOnlySpan<byte> filter = subscription.Filter;
        int qos = subscription.QoS;

        foreach (var (topic, message) in Store)
        {
            if (message.ExpiresAt < DateTime.UtcNow.Ticks)
            {
                Store.TryRemove(new(topic, message));
                continue;
            }

            if (TopicHelpers.TopicMatches(topic.Span, filter))
                writer.TryWrite(new(message.Topic, message.Payload, (QoSLevel)Math.Min(qos, (int)message.QoSLevel), true));
        }
    }

    public void OnNext([NotNull] MqttServerSessionState5 state, (byte[] Filter, bool Exists, SubscriptionOptions Options) subscription)
    {
        if (subscription is { Options.RetainDoNotSend: true } or { Options.RetainSendIfNew: true, Exists: true })
        {
            return;
        }

        ReadOnlySpan<byte> filter = subscription.Filter;
        IReadOnlyList<uint>? ids = subscription.Options.SubscriptionId is not 0 and var id ? new uint[] { id } : null;
        int qos = subscription.Options.QoS;
        var writer = state.OutgoingWriter;

        foreach (var (topic, message) in Store)
        {
            if (message.ExpiresAt < DateTime.UtcNow.Ticks)
            {
                Store.TryRemove(new(topic, message));
                continue;
            }

            if (TopicHelpers.TopicMatches(topic.Span, filter))
                writer.TryWrite(message with { QoSLevel = (QoSLevel)Math.Min(qos, (int)message.QoSLevel), SubscriptionIds = ids });
        }
    }
}