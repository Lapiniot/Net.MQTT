using System.Net.Connections;
using System.Net.WebSockets;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

internal class HttpServerWebSocketConnection : WebSocketConnection<WebSocket>
{
    private readonly TaskCompletionSource<bool> completionSource;
    private readonly IPEndPoint localEndPoint;
    private readonly IPEndPoint remoteEndPoint;

    public HttpServerWebSocketConnection(WebSocket socket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) : base(socket)
    {
        ArgumentNullException.ThrowIfNull(localEndPoint);
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        this.localEndPoint = localEndPoint;
        this.remoteEndPoint = remoteEndPoint;
        completionSource = new TaskCompletionSource<bool>();
    }

    public Task Completion => completionSource.Task;

    public override string ToString()
    {
        return $"{Id}-{nameof(HttpServerWebSocketConnection)}-{{{localEndPoint}<=>{remoteEndPoint}}}";
    }

    #region Overrides of WebSocketConnection<WebSocket>

    public override Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public override Task DisconnectAsync()
    {
        completionSource.TrySetResult(true);
        return Task.CompletedTask;
    }

    #endregion
}