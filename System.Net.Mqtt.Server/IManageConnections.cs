namespace System.Net.Mqtt.Server;

public interface IManageConnections
{
    void Abort(string clientId);
}