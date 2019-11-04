using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal interface IWebSocketAcceptedNotifier
    {
        Task NotifyAcceptedAsync(WebSocket webSocket, CancellationToken cancellationToken);
    }
}