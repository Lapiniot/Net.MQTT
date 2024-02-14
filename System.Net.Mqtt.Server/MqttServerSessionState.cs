namespace System.Net.Mqtt.Server;

/// <summary>
/// Base abstract type for server session state
/// </summary>
/// <typeparam name="TMessage">Type of the message to be used for outgoing queue processing</typeparam>
/// <typeparam name="TPubState">Type of the internal QoS1 and QoS2 delivery state</typeparam>
public abstract class MqttServerSessionState<TMessage, TPubState> : MqttSessionState<TPubState>
    where TMessage : IApplicationMessage
{
    protected MqttServerSessionState(string clientId, Channel<TMessage> outgoingChannelImpl, DateTime createdAt) : base()
    {
        ArgumentNullException.ThrowIfNull(outgoingChannelImpl);

        ClientId = clientId;
        CreatedAt = createdAt;
        (OutgoingReader, OutgoingWriter) = outgoingChannelImpl;
    }

    public DateTime CreatedAt { get; }
    protected internal ChannelReader<TMessage> OutgoingReader { get; }
    protected internal ChannelWriter<TMessage> OutgoingWriter { get; }
}

/// <summary>
/// Base abstract type for server session state
/// </summary>
/// <typeparam name="TMessage">Type of the message to be used for outgoing queue processing</typeparam>
/// <typeparam name="TPubState">Type of the internal QoS1 and QoS2 delivery state</typeparam>
/// <typeparam name="TSubscriptionState">Type of the internal subscriptions state</typeparam>
public abstract class MqttServerSessionState<TMessage, TPubState, TSubscriptionState>(
    string clientId, TSubscriptionState subscriptions,
    Channel<TMessage> outgoingChannelImpl, DateTime createdAt) :
    MqttServerSessionState<TMessage, TPubState>(clientId, outgoingChannelImpl, createdAt)
        where TMessage : IApplicationMessage
{
    public TSubscriptionState Subscriptions { get; } = subscriptions;
}