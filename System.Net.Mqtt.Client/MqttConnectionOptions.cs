namespace System.Net.Mqtt.Client
{
    public record MqttConnectionOptions(bool CleanSession = true, ushort KeepAlive = 60)
    {
        public string UserName { get; init; }
        public string Password { get; init; }
        public string LastWillTopic { get; init; }
        public Memory<byte> LastWillMessage { get; init; }
        public byte LastWillQoS { get; init; }
        public bool LastWillRetain { get; init; }
    }
}