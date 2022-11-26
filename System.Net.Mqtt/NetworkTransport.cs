using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Net.Pipelines;

namespace System.Net.Mqtt;

public sealed class NetworkTransport : IAsyncDisposable
{
    private readonly NetworkConnection connection;
    private readonly bool ownsConnection;
    private readonly NetworkTransportPipe transport;

    public NetworkTransport(NetworkConnection connection, bool ownsConnection = true)
    {
        ArgumentNullException.ThrowIfNull(connection);

        this.connection = connection;
        this.ownsConnection = ownsConnection;

        transport = new(connection);
    }

    public Task Completion => transport.InputCompletion;

    public PipeReader Input => transport.Input;

    public PipeWriter Output => transport.Output;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ownsConnection)
        {
            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        transport.Reset();
        transport.Start();
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (ownsConnection)
            {
                var vt = connection.DisconnectAsync();
                if (!vt.IsCanceled)
                {
                    await vt.ConfigureAwait(false);
                }
            }
        }
        finally
        {
            await transport.StopAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await transport.DisposeAsync().ConfigureAwait(false);
        }
        catch (ConnectionClosedException)
        {
            // Kind of expected here if connection has been already 
            // aborted by another party before e.g.
        }
        finally
        {
            if (ownsConnection)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public override string ToString() => connection.ToString();
}