using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.Hosting.Configuration;

public sealed class ServerOptions : MqttOptions
{
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    public Dictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> ListenerFactories { get; } = [];

    [Range(1, int.MaxValue)]
    public int ConnectTimeout { get; set; }

    public ProtocolLevel ProtocolLevel { get; set; }

    [ValidateObjectMembers]
    public MqttOptions5 MQTT5 { get; set; }
}

public sealed class MqttOptions5 : MqttOptions { }

public abstract class MqttOptions
{
    [Range(1, ushort.MaxValue)]
    public ushort? MaxInFlight { get; set; }

    [Range(1, ushort.MaxValue)]
    public ushort? MaxReceive { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxUnflushedBytes { get; set; }

    [Range(128, int.MaxValue)]
    public int? MaxPacketSize { get; set; }
}

[Flags]
public enum ProtocolLevel
{
#pragma warning disable CA1707
    Mqtt3 = Mqtt3_1 | Mqtt3_1_1,
    Mqtt3_1 = 0b01,
    Mqtt3_1_1 = 0b10,
    Mqtt5 = 0b100,
    Level3 = Mqtt3_1,
    Level4 = Mqtt3_1_1,
    Level5 = Mqtt5,
    All = Mqtt3_1 | Mqtt3_1_1 | Mqtt5
}

#pragma warning disable CA1812

[OptionsValidator]
internal sealed partial class ServerOptionsValidator : IValidateOptions<ServerOptions> { }