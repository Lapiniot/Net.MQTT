namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionState5 : MqttServerSessionState<MqttServerSessionSubscriptionState5, Message5>
{
    public MqttServerSessionState5(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState5(), Channel.CreateUnbounded<Message5>(), createdAt, maxInFlight)
    { }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions? options, out IReadOnlyList<uint>? subscriptionIds) =>
        Subscriptions.TopicMatches(topic, out options, out subscriptionIds);
}