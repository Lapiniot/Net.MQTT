using System.Net.Connections;

namespace System.Net.Mqtt.Server;

internal record struct ConnectionSessionContext(INetworkConnection Connection, MqttServerSession Session, Func<Task> Startup)
{
    private readonly Lazy<Task> completionLazy = new(() => Startup(), LazyThreadSafetyMode.ExecutionAndPublication);

    public Task Completion => completionLazy.Value;
}