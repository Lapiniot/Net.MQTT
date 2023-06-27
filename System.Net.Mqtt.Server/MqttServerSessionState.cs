namespace System.Net.Mqtt.Server;

/// <summary>
/// Base abstract type for server session state
/// </summary>
/// <typeparam name="TMessage">Type of the message to be used for outgoing queue processing</typeparam>
/// <typeparam name="TPubState">Type of the internal QoS1 and QoS2 delivery state</typeparam>
public abstract class MqttServerSessionState<TMessage, TPubState> : MqttSessionState<TPubState>
{
    protected MqttServerSessionState(string clientId, Channel<TMessage> outgoingChannelImpl,
        DateTime createdAt, int maxInFlight) : base(maxInFlight)
    {
        ArgumentNullException.ThrowIfNull(outgoingChannelImpl);

        ClientId = clientId;
        CreatedAt = createdAt;
        (OutgoingReader, OutgoingWriter) = outgoingChannelImpl;
    }

    public DateTime CreatedAt { get; }
    public ChannelReader<TMessage> OutgoingReader { get; }
    public ChannelWriter<TMessage> OutgoingWriter { get; }
}

/// <summary>
/// Base abstract type for server session state
/// </summary>
/// <typeparam name="TMessage">Type of the message to be used for outgoing queue processing</typeparam>
/// <typeparam name="TPubState">Type of the internal QoS1 and QoS2 delivery state</typeparam>
/// <typeparam name="TSubscriptionState">Type of the internal subscriptions state</typeparam>
public abstract class MqttServerSessionState<TMessage, TPubState, TSubscriptionState> : MqttServerSessionState<TMessage, TPubState>
    where TSubscriptionState : new()
    where TMessage : struct
{
    protected MqttServerSessionState(string clientId, TSubscriptionState subscriptions,
        Channel<TMessage> outgoingChannelImpl, DateTime createdAt, int maxInFlight) :
        base(clientId, outgoingChannelImpl, createdAt, maxInFlight) =>
        Subscriptions = subscriptions;

    public TSubscriptionState Subscriptions { get; }
}