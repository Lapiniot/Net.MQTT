using Net.Mqtt.Server.Protocol.V3;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Server;

public sealed class RetainedMessageHandler3 : RetainedMessageStore<Message3>
{
    public void OnNext([NotNull] MqttServerSessionState3 state, (byte[] Filter, byte QoS) subscription)
    {
        var writer = state.OutgoingWriter;
        ReadOnlySpan<byte> filter = subscription.Filter;
        int qos = subscription.QoS;

        foreach (var (topic, message) in Store)
        {
            if (TopicHelpers.TopicMatches(topic.Span, filter))
                writer.TryWrite(message with { QoSLevel = (QoSLevel)Math.Min(qos, (int)message.QoSLevel) });
        }
    }

    public void OnNext([NotNull] MqttServerSessionState5 state, (byte[] Filter, bool Exists, SubscriptionOptions Options) subscription)
    {
        if (subscription is { Options.RetainDoNotSend: true } or { Options.RetainSendIfNew: true, Exists: true })
            return;

        ReadOnlySpan<byte> filter = subscription.Filter;
        int qos = subscription.Options.QoS;
        IReadOnlyList<uint>? ids = subscription.Options.SubscriptionId is not 0 and var id ? new uint[] { id } : null;
        var writer = state.OutgoingWriter;

        foreach (var (topic, message) in Store)
        {
            if (TopicHelpers.TopicMatches(topic.Span, filter))
                writer.TryWrite(new(message.Topic, message.Payload, (QoSLevel)Math.Min(qos, (int)message.QoSLevel), true) { SubscriptionIds = ids });
        }
    }
}