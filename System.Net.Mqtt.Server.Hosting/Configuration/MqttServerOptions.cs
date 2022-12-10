using System.ComponentModel.DataAnnotations;
using System.Net.Connections;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class MqttServerOptions
{
    [MinLength(1)]
    public Dictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> ListenerFactories { get; } = new();

    [Range(1, int.MaxValue)]
    public int ConnectTimeout { get; set; }

    [Range(1, ushort.MaxValue)]
    public int MaxInFlight { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxUnflushedBytes { get; set; }

    public ProtocolLevel ProtocolLevel { get; set; }
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