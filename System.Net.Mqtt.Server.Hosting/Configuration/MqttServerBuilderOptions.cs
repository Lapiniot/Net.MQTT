using System.Net.Connections;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class MqttServerBuilderOptions
{
    public Dictionary<string, Func<IServiceProvider, IAsyncEnumerable<INetworkConnection>>> ListenerFactories { get; } = new();
    public int ConnectTimeout { get; set; } = 5000;
    public int DisconnectTimeout { get; set; } = 30000;
    public ProtocolLevel ProtocolLevel { get; set; } = ProtocolLevel.All;
    public int MaxPublishInFlight { get; set; } = 1024 * 16;
}

[Flags]
public enum ProtocolLevel
{
#pragma warning disable CA1707
    Mqtt3_1 = 0b01,
    Mqtt3_1_1 = 0b10,
    Level3 = Mqtt3_1,
    Level4 = Mqtt3_1_1,
    All = Mqtt3_1 | Mqtt3_1_1
}