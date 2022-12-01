namespace System.Net.Mqtt.Server;

internal record struct ConnectionSessionContext(NetworkConnection Connection, MqttServerSession Session, Func<Task> Startup)
{
    private readonly Lazy<Task> completionLazy = new(() => Startup(), LazyThreadSafetyMode.ExecutionAndPublication);

    public Task Completion => completionLazy.Value;
}