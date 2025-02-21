using OOs.Net.Connections;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public interface ITransportConnectionHandler
{
    ValueTask OnConnectedAsync(TransportConnection connection, CancellationToken cancellationToken);
}