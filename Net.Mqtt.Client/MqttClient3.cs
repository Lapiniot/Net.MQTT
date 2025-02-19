namespace Net.Mqtt.Client;

public sealed class MqttClient3 : MqttClient3Core
{
    public MqttClient3(TransportConnection connection, bool disposeConnection, string clientId, int maxInFlight) :
        base(connection, disposeConnection, clientId, maxInFlight, 0x03, "MQIsdp") =>
            ArgumentException.ThrowIfNullOrEmpty(clientId);
}