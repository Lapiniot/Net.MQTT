using System.Policies;

namespace System.Net.Mqtt.Client;

public sealed class MqttClient3 : MqttClient3Core
{
    public MqttClient3(NetworkConnection connection, string clientId, int maxInFlight, IRetryPolicy reconnectPolicy, bool disposeTransport) :
        base(connection, clientId, maxInFlight, reconnectPolicy, disposeTransport, 0x03, "MQIsdp") =>
        ArgumentException.ThrowIfNullOrEmpty(clientId);
}