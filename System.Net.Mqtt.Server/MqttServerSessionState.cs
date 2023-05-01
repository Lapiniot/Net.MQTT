namespace System.Net.Mqtt.Server;

public abstract class MqttServerSessionState : MqttSessionState
{
    protected MqttServerSessionState(string clientId, Channel<Message> outgoingChannelImpl,
        DateTime createdAt, int maxInFlight) : base(maxInFlight)
    {
        ArgumentNullException.ThrowIfNull(outgoingChannelImpl);

        ClientId = clientId;
        CreatedAt = createdAt;
        (OutgoingReader, OutgoingWriter) = outgoingChannelImpl;
    }

    public string ClientId { get; }
    public DateTime CreatedAt { get; }
    public bool IsActive { get; set; }
    public ChannelReader<Message> OutgoingReader { get; }
    public ChannelWriter<Message> OutgoingWriter { get; }

    public abstract bool TopicMatches(ReadOnlySpan<byte> span, out byte maxQoS);

    #region Overrides of MqttSessionState

    /// <inheritdoc />
    public sealed override Task<ushort> CreateMessageDeliveryStateAsync(byte flags, ReadOnlyMemory<byte> topic,
        ReadOnlyMemory<byte> payload, CancellationToken cancellationToken) =>
        base.CreateMessageDeliveryStateAsync(flags, topic, payload, cancellationToken);

    /// <inheritdoc />
    public sealed override bool DiscardMessageDeliveryState(ushort packetId) => base.DiscardMessageDeliveryState(packetId);

    #endregion
}

public abstract class MqttServerSessionState<TSubscriptionState, TMessage> : MqttServerSessionState
    where TSubscriptionState : new()
    where TMessage : struct
{
    protected MqttServerSessionState(string clientId, TSubscriptionState subscriptions,
        Channel<Message> outgoingChannelImpl, DateTime createdAt, int maxInFlight) :
        base(clientId, outgoingChannelImpl, createdAt, maxInFlight) =>
        Subscriptions = subscriptions;

    public TSubscriptionState Subscriptions { get; }

    public TMessage? WillMessage { get; set; }
}