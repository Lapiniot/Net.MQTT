using System.Net;
using System.Net.WebSockets;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public interface IAcceptedWebSocketHandler
{
    ValueTask HandleAsync(WebSocket webSocket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);
}