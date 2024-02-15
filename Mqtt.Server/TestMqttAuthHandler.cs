using Net.Mqtt.Server;

namespace Mqtt.Server;

public class TestMqttAuthHandler : IMqttAuthenticationHandler
{
    public bool Authenticate(string userName, string password) => userName == "mqtt-client" && password == "test-client";
}