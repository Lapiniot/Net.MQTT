namespace System.Net.Mqtt.Server.Features;

public interface IManageConnections
{
    void Abort(string clientId);
}