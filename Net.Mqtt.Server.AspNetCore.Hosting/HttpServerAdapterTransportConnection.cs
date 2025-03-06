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
    private readonly TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenRegistration ctRegistration;
    private int disposed;

    public override PipeReader Input => connection.Transport.Input;

    public override PipeWriter Output => connection.Transport.Output;

    public override string Id => connection.ConnectionId;

    public override EndPoint? LocalEndPoint => localEndPoint;

    public override EndPoint? RemoteEndPoint => remoteEndPoint;

    public override Task Completion => tcs.Task;

    public override string ToString() => $"{Id}-Kestrel ({LocalEndPoint}<=>{RemoteEndPoint})";

    public override void Start()
    {
        ctRegistration = connection.ConnectionClosed.UnsafeRegister((state, cancellationToken) =>
            ((TaskCompletionSource)state!).TrySetResult(), tcs);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) is not 0)
        {
            return;
        }

        using (ctRegistration)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    public override void Abort()
    {
        Output.Complete();
        Input.Complete();
    }
}