namespace System.Net.Mqtt.Server.Features;

public interface IConnectionInfoFeature
{
    IReadOnlyList<ConnectionInfo> GetConnections();
}

public record class ConnectionInfo(string ClientId, string Id, EndPoint LocalEndPoint, EndPoint RemoteEndPoint, DateTime Created);