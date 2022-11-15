using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Net.Pipelines;

namespace System.Net.Mqtt;

public sealed class NetworkConnectionAdapterTransport : NetworkTransport
{
    private readonly NetworkConnection connection;
    private readonly bool ownsConnection;
    private readonly NetworkTransportPipe transport;

    public NetworkConnectionAdapterTransport(NetworkConnection connection, bool ownsConnection = true)
    {
        ArgumentNullException.ThrowIfNull(connection);

        this.connection = connection;
        this.ownsConnection = ownsConnection;

        transport = new(connection);
    }

    public override bool IsConnected => connection.IsConnected;

    public override Task Completion => transport.InputCompletion;

    public override PipeReader Input => transport.Input;

    public override PipeWriter Output => transport.Output;

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ownsConnection)
        {
            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        await transport.ResetAsync().ConfigureAwait(false);
        transport.Start();
    }

    public override async Task DisconnectAsync()
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

    public override async ValueTask DisposeAsync()
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

    public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        connection.SendAsync(buffer, cancellationToken);

    public override string ToString() => connection.ToString();
}