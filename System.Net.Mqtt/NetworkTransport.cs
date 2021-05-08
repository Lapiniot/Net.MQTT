using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public abstract class NetworkTransport : IConnectedObject, IAsyncDisposable
    {
        public abstract PipeReader Reader { get; }
        public abstract bool IsConnected { get; }
        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
        public abstract Task DisconnectAsync();
        public abstract ValueTask DisposeAsync();
        public abstract ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}