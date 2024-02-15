namespace Net.Mqtt.Client;

public record MqttConnectionOptions3(bool CleanSession = true, ushort KeepAlive = 60)
{
    public string UserName { get; init; }
    public string Password { get; init; }
    public string LastWillTopic { get; init; }
    public Memory<byte> LastWillMessage { get; init; }
    public QoSLevel LastWillQoS { get; init; }
    public bool LastWillRetain { get; init; }

    public static MqttConnectionOptions3 Default { get; } = new();
}