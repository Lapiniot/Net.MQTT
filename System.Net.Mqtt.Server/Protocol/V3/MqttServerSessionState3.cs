
namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState3 : MqttServerSessionState<MqttServerSessionSubscriptionState3, Message>
{
    public MqttServerSessionState3(string clientId, DateTime createdAt, int maxInFlight) :
        this(clientId, new MqttServerSessionSubscriptionState3(), Channel.CreateUnbounded<Message>(), createdAt, maxInFlight)
    { }

    protected MqttServerSessionState3(string clientId, MqttServerSessionSubscriptionState3 subscriptions,
        Channel<Message> outgoingChannelImpl, DateTime createdAt, int maxInFlight) :
        base(clientId, subscriptions, outgoingChannelImpl, createdAt, maxInFlight)
    { }

    public sealed override bool TopicMatches(ReadOnlySpan<byte> span, out byte maxQoS) =>
        Subscriptions.TopicMatches(span, out maxQoS);

    public sealed override void Trim()
    {
        Subscriptions.Trim();
        base.Trim();
    }
}