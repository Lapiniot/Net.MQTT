namespace Net.Mqtt.Client;

#nullable enable

public record class Message(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload,
    QoSLevel QoSLevel = QoSLevel.QoS0, bool Retain = false) : IApplicationMessage
{
    public uint? ExpiryInterval { get; init; }
    public bool PayloadFormat { get; init; }
    public ReadOnlyMemory<byte> ContentType { get; init; }
    public ReadOnlyMemory<byte> ResponseTopic { get; init; }
    public ReadOnlyMemory<byte> CorrelationData { get; init; }
    public IReadOnlyList<UserProperty>? UserProperties { get; init; }
}