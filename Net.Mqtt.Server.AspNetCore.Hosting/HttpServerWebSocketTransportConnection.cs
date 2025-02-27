using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using OOs.Net.Connections;

#nullable enable

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public sealed class HttpServerWebSocketTransportConnection(WebSocket webSocket,
    IPEndPoint localEndPoint, IPEndPoint remoteEndPoint,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    WebSocketTransportConnection(webSocket, localEndPoint, remoteEndPoint, inputPipeOptions, outputPipeOptions)
{
    private readonly TaskCompletionSource completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override Task Completion => completionSource.Task;

    protected override async ValueTask OnStoppingAsync()
    {
        try
        {
            await base.OnStoppingAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            completionSource.TrySetException(e);
            throw;
        }
        finally
        {
            completionSource.TrySetResult();
        }
    }

    public override string ToString() => $"{Id}-Kestrel-WS ({LocalEndPoint}<=>{RemoteEndPoint})";
}