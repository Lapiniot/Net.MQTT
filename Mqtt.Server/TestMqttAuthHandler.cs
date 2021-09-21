using System.Net.Mqtt.Server;

namespace Mqtt.Server;

public class TestMqttAuthHandler : IMqttAuthenticationHandler
{
    public bool Authenticate(string userName, string password)
    {
        return userName == "mqtt-client" && password == "test-client";
    }
}