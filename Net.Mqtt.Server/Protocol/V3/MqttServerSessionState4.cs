namespace Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSessionState4(string clientId, DateTime createdAt) :
    MqttServerSessionState3(clientId, new MqttServerSessionSubscriptionState4(), Channel.CreateUnbounded<Message3>(), createdAt)
{ }