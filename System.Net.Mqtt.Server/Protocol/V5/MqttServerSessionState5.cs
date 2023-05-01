using System.Net.Mqtt.Server.Protocol.V3;

namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionState5 : MqttServerSessionState3
{
    public MqttServerSessionState5(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState5(), Channel.CreateUnbounded<Message>(), createdAt, maxInFlight)
    { }
}