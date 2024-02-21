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

    /// <summary>
    /// Time for server to wait for the valid CONNECT packet from client.
    /// </summary>
    [Range(50, int.MaxValue)]
    public int ConnectTimeoutMilliseconds { get; set; }

    /// <summary>
    /// Allowed protocol versions the server must support and accept connections.
    /// </summary>
    [EnumDataType(typeof(ProtocolLevel))]
    public ProtocolLevel ProtocolLevel { get; set; }

    /// <summary>
    /// MQTT option overrides applied to MTTT5 sessions specifically.
    /// </summary>
    [ValidateObjectMembers]
    public MqttOptions5 MQTT5 { get; set; }
}

/// <summary>
/// Represents a set of options applicable to MQTT5 protocol only.
/// </summary>
public sealed class MqttOptions5 : MqttOptions
{
    /// <summary>
    /// Topic size threshold considered by the server as big enough to apply topic/alias mapping if client supports.
    /// </summary>
    [Range(1, ushort.MaxValue)]
    public ushort TopicAliasSizeThreshold { get; set; }
}

/// <summary>
/// Represents a set of generic options applicable to all protocol versions.
/// </summary>
public class MqttOptions
{
    /// <summary>
    /// Maximum number of outgoing QoS1/QoS2 publications server is willing to process concurrently.
    /// </summary>
    [Range(1, ushort.MaxValue)]
    public ushort? MaxInFlight { get; set; }

    /// <summary>
    /// Maximum number of incoming QoS1/QoS2 publications server is ready to accept from client concurrently.
    /// </summary>
    [Range(1, ushort.MaxValue)]
    public ushort? MaxReceive { get; set; }

    /// <summary>
    /// Maximum bytes server is allowed to accumulate in a send buffer before flushing to the client.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? MaxUnflushedBytes { get; set; }

    /// <summary>
    /// Maximum MQTT packet size server is willing to accept from the client.
    /// </summary>
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