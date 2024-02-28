namespace Net.Mqtt.Client;

public record MqttConnectionOptions5(bool CleanStart = false, ushort KeepAlive = 60)
{
    public string UserName { get; init; }
    public string Password { get; init; }
    public string LastWillTopic { get; init; }
    public Memory<byte> LastWillMessage { get; init; }
    public QoSLevel LastWillQoS { get; init; }
    public bool LastWillRetain { get; init; }
    public ushort ReceiveMaximum { get; init; } = ushort.MaxValue;
    public int MaxPacketSize { get; init; } = int.MaxValue;
    public ushort TopicAliasMaximum { get; init; }
    public uint SessionExpiryInterval { get; init; }
    public ReadOnlyMemory<byte> AuthenticationData { get; init; }
    public ReadOnlyMemory<byte> AuthenticationMethod { get; init; }
    public bool RequestProblem { get; init; }
    public bool RequestResponse { get; init; }
    public IReadOnlyList<(ReadOnlyMemory<byte> Name, ReadOnlyMemory<byte> Value)> UserProperties { get; init; }
    public ReadOnlyMemory<byte> WillContentType { get; init; }
    public ReadOnlyMemory<byte> WillResponseTopic { get; init; }
    public uint WillDelayInterval { get; init; }
    public uint? WillExpiryInterval { get; init; }
    public ReadOnlyMemory<byte> WillCorrelationData { get; init; }
    public bool WillPayloadFormat { get; init; }
    public IReadOnlyList<(ReadOnlyMemory<byte> Name, ReadOnlyMemory<byte> Value)> WillUserProperties { get; init; }

    public static MqttConnectionOptions5 Default { get; } = new();
}