namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionState5 : MqttServerSessionState<MqttServerSessionSubscriptionState5, Message5>
{
    public MqttServerSessionState5(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState5(), Channel.CreateUnbounded<Message5>(), createdAt, maxInFlight)
    { }

    public override bool TopicMatches(ReadOnlySpan<byte> topic, out byte maxQoS) => Subscriptions.TopicMatches(topic, out maxQoS);
}