namespace Net.Mqtt.Server;

internal sealed record ConnectionSessionContext(NetworkConnection Connection, MqttServerSession Session,
    ILogger<MqttServer> Logger, DateTime Created, CancellationToken ServerStopping)
{
    private readonly object syncLock = new();
    private volatile Task? task;

    /// <summary>
    /// Starts <see cref="Session" /> on the current <see cref="Connection" /> and waits for its completion
    /// </summary>
    /// <remarks>This method ensures run-once semantics. All subsequent calls will 
    /// return the same task which is safe to be awaited multiple times to 
    /// know whether the session has completely finished processing.</remarks>
    /// <returns><see cref="Task" /> which is completed when session is over</returns>
    public Task RunAsync()
    {
        if (task is not null)
            return task;

        lock (syncLock)
        {
            return task ??= RunCoreAsync();
        }
    }

    private async Task RunCoreAsync()
    {
        var session = Session;
        var stoppingToken = ServerStopping;

        Logger.LogSessionStarting(session);

        try
        {
            var task = session.RunAsync(stoppingToken);
            if (!task.IsCanceled)
                Logger.LogSessionStarted(session);
            await task.ConfigureAwait(false);
        }
        finally
        {
            if (Session.DisconnectReceived)
            {
                if (Session.DisconnectReason is DisconnectReason.Normal)
                    Logger.LogSessionTerminatedGracefully(session);
                else
                    Logger.LogSessionAbortedByClient(session, session.DisconnectReason);
            }
            else
            {
                if (Session.DisconnectReason is DisconnectReason.Normal)
                    Logger.LogConnectionAbortedByClient(session);
                else
                    Logger.LogSessionAbortedForcibly(session, session.DisconnectReason);
            }
        }
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