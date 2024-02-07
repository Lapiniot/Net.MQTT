namespace System.Net.Mqtt;

#nullable enable

public readonly record struct Message3(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, QoSLevel QoSLevel, bool Retain) : IApplicationMessage;

public record class Message5(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, QoSLevel QoSLevel, bool Retain) : IApplicationMessage
{
    public long? ExpiresAt { get; init; }
    public byte PayloadFormat { get; init; }
    public ReadOnlyMemory<byte> ContentType { get; init; }
    public ReadOnlyMemory<byte> ResponseTopic { get; init; }
    public ReadOnlyMemory<byte> CorrelationData { get; init; }
    public IReadOnlyList<uint>? SubscriptionIds { get; init; }
    public IReadOnlyList<Utf8StringPair>? UserProperties { get; init; }
}