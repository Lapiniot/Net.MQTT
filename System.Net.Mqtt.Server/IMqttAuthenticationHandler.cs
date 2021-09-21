namespace System.Net.Mqtt.Server;

public interface IMqttAuthenticationHandler
{
    bool Authenticate(string userName, string password);
}