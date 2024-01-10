using System.Net.WebSockets;
using OOs.Net.Connections;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

internal sealed class HttpServerWebSocketConnection(WebSocket acceptedSocket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) :
    WebSocketServerConnection(acceptedSocket, localEndPoint, remoteEndPoint)
{
    private readonly TaskCompletionSource completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Completion => completionSource.Task;

    public override string ToString() => $"{Id}-WS ({LocalEndPoint}<=>{RemoteEndPoint})";

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