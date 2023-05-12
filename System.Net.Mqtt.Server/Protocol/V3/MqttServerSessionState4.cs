namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSessionState4 : MqttServerSessionState3
{
    public MqttServerSessionState4(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState4(), Channel.CreateUnbounded<Message3>(), createdAt, maxInFlight)
    { }
}