using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;
using static System.Net.Mqtt.Server.Protocol.V5.RetainHandling;

namespace System.Net.Mqtt.Server;

public sealed class RetainedMessageHandler5 : RetainedMessageStore<Message5>
{
    public void OnNext([NotNull] MqttServerSessionState3 state, (byte[] Filter, byte QoS) subscription)
    {
        var writer = state.OutgoingWriter;
        ReadOnlySpan<byte> filter = subscription.Filter;
        var qos = subscription.QoS;

        foreach (var (topic, message) in Store)
        {
            if (MqttExtensions.TopicMatches(topic.Span, filter))
                writer.TryWrite(new(message.Topic, message.Payload, Math.Min(qos, message.QoSLevel), true));
        }
    }

    public void OnNext([NotNull] MqttServerSessionState5 state, (byte[] Filter, bool Exists, SubscriptionOptions Options) subscription)
    {
        if (subscription is { Options.RetainHandling: DoNotSend } or { Options.RetainHandling: SendIfNew, Exists: true })
        {
            return;
        }

        ReadOnlySpan<byte> filter = subscription.Filter;
        IReadOnlyList<uint>? ids = subscription.Options.SubscriptionId is not 0 and var id ? new uint[] { id } : null;
        var qos = subscription.Options.QoS;
        var writer = state.OutgoingWriter;

        foreach (var (topic, message) in Store)
        {
            if (MqttExtensions.TopicMatches(topic.Span, filter))
                writer.TryWrite(message with { QoSLevel = Math.Min(qos, message.QoSLevel), SubscriptionIds = ids });
        }
    }
}