namespace Net.Mqtt.Client;

public sealed class MqttClient3 : MqttClient3Core
{
    public MqttClient3(NetworkConnection connection, bool disposeConnection,
        string clientId, int maxInFlight, IRetryPolicy? reconnectPolicy) :
        base(connection, disposeConnection, clientId, maxInFlight, reconnectPolicy, 0x03, "MQIsdp") =>
            ArgumentException.ThrowIfNullOrEmpty(clientId);
}