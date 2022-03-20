using System.Net.Connections;
using System.Net.WebSockets;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

internal sealed class HttpServerWebSocketConnection : WebSocketServerConnection
{
    private readonly TaskCompletionSource completionSource;
    private readonly IPEndPoint localEndPoint;

    public HttpServerWebSocketConnection(WebSocket acceptedSocket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) :
        base(acceptedSocket, remoteEndPoint)
    {
        ArgumentNullException.ThrowIfNull(localEndPoint);
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        this.localEndPoint = localEndPoint;
        completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task Completion => completionSource.Task;

    public override string ToString() => $"{Id}-{nameof(HttpServerWebSocketConnection)}-{{{localEndPoint}<=>{RemoteEndPoint}}}";

    protected override async Task StoppingAsync()
    {
        try
        {
            await base.StoppingAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completionSource.TrySetException(exception);
            throw;
        }
        finally
        {
            completionSource.TrySetResult();
        }
    }
}