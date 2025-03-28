using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using OOs.Net.Connections;

#nullable enable

namespace Net.Mqtt.Server.Hosting;

public sealed class MqttServerOptions : MqttOptions
{
    [MinLength(1, ErrorMessage = "At least one endpoint must be configured.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Dictionary<string, MqttEndpoint> Endpoints { get; } = [];

    public Dictionary<string, CertificateOptions> Certificates { get; } = [];

    /// <summary>
    /// Time for server to wait for the valid CONNECT packet from client.
    /// </summary>
    [Range(50, int.MaxValue)]
    public int ConnectTimeoutMilliseconds { get; set; } = 1500;

    /// <summary>
    /// Allowed protocol versions the server must support and accept connections.
    /// </summary>
    [EnumDataType(typeof(ProtocolLevel))]
    public ProtocolLevel ProtocolLevel { get; set; } = ProtocolLevel.All;

    /// <summary>
    /// MQTT option overrides applied to MTTT5 sessions specifically.
    /// </summary>
    [ValidateObjectMembers]
    public MqttOptions5 MQTT5 { get; } = new();
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
    public ushort TopicAliasSizeThreshold { get; set; } = 128;

    /// <summary>
    /// This value indicates the highest value that the Server will accept as a Topic Alias sent by the Client. 
    /// The Server uses this value to limit the number of Topic Aliases that it is willing to hold on this Connection.
    /// </summary>
    public ushort TopicAliasMax { get; set; }
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
    public ushort MaxInFlight { get; set; } = 32767;

    /// <summary>
    /// Maximum number of incoming QoS1/QoS2 publications server is ready to accept from client concurrently.
    /// </summary>
    [Range(1, ushort.MaxValue)]
    public ushort MaxReceive { get; set; } = 32767;

    /// <summary>
    /// Maximum bytes server is allowed to accumulate in a send buffer before flushing to the client.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxUnflushedBytes { get; set; } = 4 * 1024;

    /// <summary>
    /// Maximum MQTT packet size server is willing to accept from the client.
    /// </summary>
    [Range(128, int.MaxValue)]
    public int MaxPacketSize { get; set; } = int.MaxValue;
}

public sealed class MqttEndpoint
{
    private readonly Func<IAsyncEnumerable<TransportConnection>>? factory;

    public MqttEndpoint() { }
    public MqttEndpoint(EndPoint endPoint) => EndPoint = endPoint;
    public MqttEndpoint(Func<IAsyncEnumerable<TransportConnection>> factory) => this.factory = factory;

    public Uri? Url { get; set; }
    public CertificateOptions? Certificate { get; set; }
    public SslProtocols SslProtocols { get; set; } = SslProtocols.None;
    public ClientCertificateMode? ClientCertificateMode { get; set; }
    public EndPoint? EndPoint { get; }
    public bool? UseQuic { get; internal set; }

    // This is just a dumb workaround to calm down the ConfigurationBindingGenerator 
    // which doesn't skip property binding with unsupported types. 
    // Otherwise this would be a simple readonly property.
    // TODO: check necessity of this trick in the upcoming .NET releases!
#pragma warning disable CA1024 // Use properties where appropriate
    public Func<IAsyncEnumerable<TransportConnection>>? GetFactory() => factory;
#pragma warning restore CA1024 // Use properties where appropriate
}

public sealed class CertificateOptions
{
    private readonly Func<X509Certificate2>? loader;

    public CertificateOptions() { }
    public CertificateOptions(Func<X509Certificate2> loader) => this.loader = loader;

    public StoreLocation Location { get; set; } = StoreLocation.CurrentUser;
    public StoreName Store { get; set; } = StoreName.My;
    public string? Subject { get; set; }
    public string? Path { get; set; }
    public string? KeyPath { get; set; }
    public string? Password { get; set; }
    public bool AllowInvalid { get; set; }

#pragma warning disable CA1024 // Use properties where appropriate
    public Func<X509Certificate2>? GetLoader() => loader;
#pragma warning restore CA1024 // Use properties where appropriate
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

public enum ClientCertificateMode
{
    NoCertificate = 0,
    AllowCertificate = 1,
    RequireCertificate = 2
}

#pragma warning disable CA1812

[OptionsValidator]
internal sealed partial class MqttServerOptionsValidator : IValidateOptions<MqttServerOptions> { }