namespace Net.Mqtt.Server;

public interface IMqttAuthenticationHandler
{
    ValueTask<bool> AuthenticateAsync(string userName, string password);
}