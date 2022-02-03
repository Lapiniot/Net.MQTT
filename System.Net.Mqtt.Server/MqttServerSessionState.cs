namespace System.Net.Mqtt.Server;

public abstract class MqttServerSessionState : MqttSessionState
{
    protected MqttServerSessionState(string clientId, DateTime createdAt)
    {
        ClientId = clientId;
        CreatedAt = createdAt;
    }

    public string ClientId { get; }
    public DateTime CreatedAt { get; }

    public bool IsActive { get; set; }

    #region Subscription state management

    public abstract bool TopicMatches(string topic, out byte maxQoS);

    public abstract byte[] Subscribe(IReadOnlyList<(string Topic, byte QoS)> filters);

    public abstract void Unsubscribe(IReadOnlyList<string> filters);

    #endregion

    #region Incoming message delivery state

    public abstract bool TryEnqueueMessage(Message message);

    public abstract ValueTask<Message> DequeueMessageAsync(CancellationToken cancellationToken);

    #endregion
}