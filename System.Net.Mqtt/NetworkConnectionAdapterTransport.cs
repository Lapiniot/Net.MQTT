using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Net.Pipelines;

namespace System.Net.Mqtt;

public sealed class NetworkConnectionAdapterTransport : NetworkTransport
{
    private readonly INetworkConnection connection;
    private readonly bool ownsConnection;
    private readonly NetworkPipeReader reader;

    public NetworkConnectionAdapterTransport(INetworkConnection connection, bool ownsConnection = true)
    {
        ArgumentNullException.ThrowIfNull(connection);

        this.connection = connection;
        this.ownsConnection = ownsConnection;

        reader = new NetworkPipeReader(connection);
    }

    public override PipeReader Reader => reader.Reader;

    public override bool IsConnected => connection.IsConnected;

    public override Task Completion => reader.Completion;

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if(ownsConnection)
        {
            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        await reader.ResetAsync().ConfigureAwait(false);
        reader.Start();
    }

    public override async Task DisconnectAsync()
    {
        try
        {
            if(ownsConnection)
            {
                var vt = connection.DisconnectAsync();
                if(!vt.IsCanceled)
                {
                    await vt.ConfigureAwait(false);
                }
            }
        }
        finally
        {
            var vt = reader.StopAsync();
            if(!vt.IsCompletedSuccessfully)
            {
                await vt.ConfigureAwait(false);
            }
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
        catch(ConnectionAbortedException)
        {
            // Kind of expected here if connection has been already 
            // aborted by another party before e.g.
        }
        finally
        {
            if(ownsConnection)
            {
                var vt = connection.DisposeAsync();
                if(!vt.IsCompletedSuccessfully)
                {
                    await vt.ConfigureAwait(false);
                }
            }
        }
    }

    public override ValueTask SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return connection.SendAsync(buffer, cancellationToken);
    }
}