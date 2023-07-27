namespace System.Net.Mqtt.Server;

[Flags]
public enum MqttProtocol
{
    Level3 = 0b001,
    Level4 = 0b010,
    Level5 = 0b100
}

public sealed record MqttServerOptions : ProtocolOptions
{
    public required TimeSpan ConnectTimeout { get; init; }
    public required MqttProtocol Protocols { get; init; }
    public IMqttAuthenticationHandler? AuthenticationHandler { get; init; }
    public required ProtocolOptions5 MQTT5 { get; init; }
}

public sealed record ProtocolOptions5 : ProtocolOptions { }

public record ProtocolOptions
{
    public required ushort MaxInFlight { get; init; }
    public required ushort MaxReceive { get; init; }
    public required int MaxUnflushedBytes { get; init; }
}