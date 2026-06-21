using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

/// <summary>
/// Provides simple <see cref="ConnectionContext"/> adapting wrapper for the 
/// <see cref="OOs.Net.Connections.TransportConnection"/>.
/// </summary>
public sealed class HttpServerAdapterTransportConnection : TransportConnection, ITransportConnectionLifetime
{
    private readonly TaskCompletionSource connectionClosedTcs;
    private readonly TaskCompletionSource completedTcs;
    private readonly CancellationTokenRegistration connectionClosedCtr;
    private readonly ConnectionContext connection;
    private readonly EndPoint localEndPoint;
    private readonly EndPoint remoteEndPoint;
    private int disposed;

    public HttpServerAdapterTransportConnection([NotNull] ConnectionContext connection, EndPoint localEndPoint, EndPoint remoteEndPoint)
    {
        this.connection = connection;
        this.localEndPoint = localEndPoint;
        this.remoteEndPoint = remoteEndPoint;

        completedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionClosedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionClosedCtr = connection.ConnectionClosed.UnsafeRegister(ConnectionClosed, state: connectionClosedTcs);

        static void ConnectionClosed(object? state, CancellationToken _) => ((TaskCompletionSource)state!).TrySetResult();
    }

    public override PipeReader Input => connection.Transport.Input;

    public override PipeWriter Output => connection.Transport.Output;

    public override string Id => connection.ConnectionId;

    public override EndPoint? LocalEndPoint => localEndPoint;

    public override EndPoint? RemoteEndPoint => remoteEndPoint;

    public override Task ConnectionClosed => connectionClosedTcs.Task;

    public Task Completed => completedTcs.Task;

    public override string ToString() => $"{Id}-Kestrel ({LocalEndPoint}<=>{RemoteEndPoint})";

    public override ValueTask StartAsync(CancellationToken cancellationToken) => default;

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) is not 0)
        {
            return;
        }

        try
        {
            using (connectionClosedCtr)
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            completedTcs.SetResult();
        }
    }

    public override void Abort()
    {
        connection.Abort();
    }

    public override async ValueTask CloseOutputAsync()
    {
        if (connection.Features.Get<ISslStreamFeature>() is ISslStreamFeature sslFeature)
        {
            await sslFeature.SslStream.ShutdownAsync().ConfigureAwait(false);
        }

        await Output.CompleteAsync().ConfigureAwait(false);
    }
}