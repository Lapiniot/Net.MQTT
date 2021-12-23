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

    public abstract byte[] Subscribe((string Filter, byte QosLevel)[] filters);

    public abstract void Unsubscribe(string[] filters);

    #endregion

    #region Incoming message delivery state

    public abstract ValueTask EnqueueAsync(Message message, CancellationToken cancellationToken);

    public abstract ValueTask<Message> DequeueAsync(CancellationToken cancellationToken);

    #endregion
}