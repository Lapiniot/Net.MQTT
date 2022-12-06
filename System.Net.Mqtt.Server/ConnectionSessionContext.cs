namespace System.Net.Mqtt.Server;

internal record struct ConnectionSessionContext(NetworkConnection Connection, MqttServerSession Session, Func<MqttServerSession, Task> DeferredStartup)
{
    private readonly Lazy<Task> completionLazy = new(() => DeferredStartup(Session), LazyThreadSafetyMode.ExecutionAndPublication);

    public Task Completion => completionLazy.Value;

    public void Abort()
    {
        Session.Abort();
        Connection.DisconnectAsync();
    }
}