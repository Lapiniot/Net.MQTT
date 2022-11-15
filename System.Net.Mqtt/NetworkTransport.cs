using System.IO.Pipelines;

namespace System.Net.Mqtt;

public abstract class NetworkTransport : IDuplexPipe, IConnectedObject, IAsyncDisposable
{
    public abstract Task Completion { get; }
    public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    public abstract ValueTask DisposeAsync();
    public abstract bool IsConnected { get; }
    public abstract PipeReader Input { get; }
    public abstract PipeWriter Output { get; }
    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
    public abstract Task DisconnectAsync();
}