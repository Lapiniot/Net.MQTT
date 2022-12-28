namespace System.Net.Mqtt.Server;

internal record struct ConnectionSessionContext(NetworkConnection Connection, MqttServerSession Session,
    Func<MqttServerSession, CancellationToken, Task> DeferredStartup, CancellationToken SessionAborted)
{
    private readonly object syncLock = new();
    private Task task;

    public Task RunAsync()
    {
        if (task is not null)
        {
            return task;
        }

        lock (syncLock)
        {
            return task ??= DeferredStartup(Session, SessionAborted);
        }
    }

    public void Abort()
    {
        Session.Abort();
        Connection.DisconnectAsync();
    }
}