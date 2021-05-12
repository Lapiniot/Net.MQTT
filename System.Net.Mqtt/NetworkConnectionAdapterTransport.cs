using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public sealed class NetworkConnectionAdapterTransport : NetworkTransport
    {
        private readonly INetworkConnection connection;
        private readonly bool disposeConnection;
        private readonly NetworkPipeReader reader;

        public NetworkConnectionAdapterTransport(INetworkConnection connection, bool disposeConnection = false)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.disposeConnection = disposeConnection;
            this.reader = new NetworkPipeReader(connection);
        }

        public override PipeReader Reader => reader;

        public override bool IsConnected => connection.IsConnected;

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            this.reader.Start();
        }

        public override async Task DisconnectAsync()
        {
            var vt = reader.StopAsync();
            if(!vt.IsCompletedSuccessfully)
            {
                await vt.ConfigureAwait(false);
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                var vt = reader.DisposeAsync();
                if(!vt.IsCompletedSuccessfully)
                {
                    await vt.ConfigureAwait(false);
                }
            }
            finally
            {
                if(disposeConnection)
                {
                    var vt = connection.DisposeAsync();
                    if(!vt.IsCompletedSuccessfully)
                    {
                        await vt.ConfigureAwait(false);
                    }
                }
            }
        }

        public override ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return connection.SendAsync(buffer, cancellationToken);
        }

        public override string ToString()
        {
            return connection.ToString();
        }
    }
}