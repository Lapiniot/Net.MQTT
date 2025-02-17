using System.Net;

namespace Net.Mqtt.Server.Features;

public interface IConnectionInfoFeature
{
    IEnumerable<ConnectionInfo> GetConnections();
}

public record class ConnectionInfo(string ClientId, string Id, EndPoint? LocalEndPoint, EndPoint? RemoteEndPoint, DateTime Created);