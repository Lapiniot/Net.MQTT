using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public interface IAcceptedWebSocketQueue
    {
        ValueTask<Task> EnqueueAsync(WebSocket webSocket, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);
    }
}