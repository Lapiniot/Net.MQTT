namespace System.Net.Mqtt.Server;

[Flags]
public enum MqttProtocols
{
    Level3 = 0b001,
    Level4 = 0b010,
    Level5 = 0b100
}

public record class MqttServerOptions
{
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public ushort MaxInFlight { get; init; } = (ushort)short.MaxValue;
    public ushort MaxReceive5 { get; init; } = (ushort)short.MaxValue;
    public int MaxUnflushedBytes { get; set; } = 8096;
    public MqttProtocols Protocols { get; init; } = MqttProtocols.Level3 | MqttProtocols.Level4 | MqttProtocols.Level5;
    public IMqttAuthenticationHandler? AuthenticationHandler { get; init; }
}