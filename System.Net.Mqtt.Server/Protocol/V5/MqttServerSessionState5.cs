namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionState5 : MqttServerSessionState<Message5, Message5, MqttServerSessionSubscriptionState5>
{
    public MqttServerSessionState5(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState5(), Channel.CreateUnbounded<Message5>(), createdAt, maxInFlight)
    { }

    public bool TopicMatches(ReadOnlySpan<byte> topic, [NotNullWhen(true)] out SubscriptionOptions? options, out IReadOnlyList<uint>? subscriptionIds) =>
        Subscriptions.TopicMatches(topic, out options, out subscriptionIds);

    public Task<ushort> CreateMessageDeliveryStateAsync(Message5 message, CancellationToken cancellationToken)
        => CreateDeliveryStateCoreAsync(message, cancellationToken);

    public void DiscardMessageDeliveryState(ushort id) => DiscardDeliveryStateCore(id);
}