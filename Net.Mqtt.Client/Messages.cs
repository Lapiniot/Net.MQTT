namespace Net.Mqtt.Client;

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

/// <summary>
/// Represents generic incoming message with properties common across all MQTT protocol versions.
/// </summary>
/// <param name="Topic">Message topic.</param>
/// <param name="Payload">Message payload bytes.</param>
/// <param name="Retained">Retain flag value.</param>
public readonly record struct MqttMessage(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, bool Retained);

/// <summary>
/// Represents extended incoming message with properties specific to MQTT5 protocol
/// </summary>
/// <param name="Topic">Message topic.</param>
/// <param name="Payload">Message payload bytes.</param>
/// <param name="Retained">Retain flag value.</param>
public readonly record struct MqttMessage5(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, bool Retained)
{
    public bool PayloadFormat { get; init; }
    public DateTimeOffset Expires { get; init; }
    public ReadOnlyMemory<byte> ContentType { get; init; }
    public ReadOnlyMemory<byte> ResponseTopic { get; init; }
    public ReadOnlyMemory<byte> CorrelationData { get; init; }
    public IReadOnlyList<UserProperty> UserProperties { get; init; }
    public IReadOnlyList<uint> SubscriptionIds { get; init; }
}

public delegate void MessageReceivedHandler<T>(object sender, MqttMessageArgs<T> e);

/// <summary>
/// Ref struct wrapper for generic message reference.
/// </summary>
/// <remarks>
/// Exists primarily for performance reasons to avoid exessive struct copy overhead
/// when passed as event listener delegate parameter e.g.
/// </remarks>
/// <typeparam name="T">Generic message type.</typeparam>
public readonly ref struct MqttMessageArgs<T>
{
    private readonly ref T message;

    public MqttMessageArgs(ref readonly T message)
    {
        if (Unsafe.IsNullRef(in message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        this.message = ref Unsafe.AsRef(in message);
    }

    public readonly ref T Message => ref message;
}