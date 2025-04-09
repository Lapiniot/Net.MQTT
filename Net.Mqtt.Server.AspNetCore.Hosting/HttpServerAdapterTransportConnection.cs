using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

/// <summary>
/// Provides simple <see cref="ConnectionContext"/> adapting wrapper for the 
/// <see cref="OOs.Net.Connections.TransportConnection"/>.
/// </summary>
/// <param name="connection">The <see cref="ConnectionContext"/>.</param>
/// <param name="localEndPoint">Connection's LocalEndPoint.</param>
/// <param name="remoteEndPoint">Connection's RemoteEndPoint.</param>
public sealed class HttpServerAdapterTransportConnection(ConnectionContext connection,
    EndPoint localEndPoint, EndPoint remoteEndPoint) : TransportConnection
{
    private readonly TaskCompletionSource completionTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int disposed;

    public override PipeReader Input => connection.Transport.Input;

    public override PipeWriter Output => connection.Transport.Output;

    public override string Id => connection.ConnectionId;

    public override EndPoint? LocalEndPoint => localEndPoint;

    public override EndPoint? RemoteEndPoint => remoteEndPoint;

    public override Task Completion => completionTcs.Task;

    public override string ToString() => $"{Id}-Kestrel ({LocalEndPoint}<=>{RemoteEndPoint})";

    public override ValueTask StartAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) is not 0)
        {
            return;
        }

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            completionTcs.TrySetResult();
        }
    }

    public override void Abort()
    {
        connection.Transport.Output.Complete();
        connection.Transport.Input.Complete();
    }
}