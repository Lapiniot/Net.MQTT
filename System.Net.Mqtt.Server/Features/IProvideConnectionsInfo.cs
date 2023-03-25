namespace System.Net.Mqtt.Server.Features;

public interface IProvideConnectionsInfo
{
    IReadOnlyList<ConnectionInfo> GetConnections();
}

public record class ConnectionInfo(string ClientId, string Id, EndPoint LocalEndPoint, EndPoint RemoteEndPoint, DateTime Created);