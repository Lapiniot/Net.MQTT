namespace System.Net.Mqtt.Server;

internal sealed record ConnectionSessionContext(NetworkConnection Connection, MqttServerSession Session,
    ILogger<MqttServer> Logger, DateTime Created, CancellationToken ServerStopping)
{
    private readonly object syncLock = new();
    private Task task;

    /// <summary>
    /// Starts <see cref="Session" /> on the current <see cref="Connection" /> and waits for its completion
    /// </summary>
    /// <remarks>This method ensures run-once semantics. All subsequent calls will 
    /// return the same task which is safe to be awaited multiple times to 
    /// know whether the session has completely finished processing.</remarks>
    /// <returns><see cref="Task" /> which is completed when session is over</returns>
    public Task WaitCompletedAsync()
    {
        if (task is not null)
        {
            return task;
        }

        lock (syncLock)
        {
            return task ??= RunCoreAsync(Session, ServerStopping);
        }
    }

    private async Task RunCoreAsync(MqttServerSession session, CancellationToken stoppingToken)
    {
        Logger.LogSessionStarting(session);

        try
        {
            try
            {
                await session.StartAsync(stoppingToken).ConfigureAwait(false);
                Logger.LogSessionStarted(session);
                await session.WaitCompletedAsync(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                await session.StopAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogSessionAbortedForcibly(session);
            return;
        }
        catch (ConnectionClosedException)
        {
            // expected
        }

        if (session.DisconnectReceived)
        {
            Logger.LogSessionTerminatedGracefully(session);
        }
        else
        {
            Logger.LogConnectionAbortedByClient(session);
        }
    }

    public void Abort()
    {
        Session.Abort();
        Connection.DisconnectAsync();
    }

    public void Deconstruct(out NetworkConnection connection, out MqttServerSession session, out DateTime created)
    {
        connection = Connection;
        session = Session;
        created = Created;
    }

    public void Deconstruct(out NetworkConnection connection, out MqttServerSession session)
    {
        connection = Connection;
        session = Session;
    }
}