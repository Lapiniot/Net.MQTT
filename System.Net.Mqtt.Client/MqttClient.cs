namespace System.Net.Mqtt.Client;

#pragma warning disable CA1003
public delegate void MessageReceivedHandler(object sender, in MqttMessage message);

public abstract class MqttClient : MqttClientSession
{
    private readonly ObserversContainer<MqttMessage> publishObservers;

    protected internal MqttClient(string clientId, NetworkTransportPipe transport, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        publishObservers = new();
        ClientId = clientId;
    }

    public event EventHandler<ConnectedEventArgs> Connected;
    public event EventHandler<DisconnectedEventArgs> Disconnected;
    public event MessageReceivedHandler MessageReceived;

    public string ClientId { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task ConnectAsync(CancellationToken cancellationToken = default) => StartActivityAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task DisconnectAsync() => StopActivityAsync();

    public abstract Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default);

    public abstract Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default);

    public abstract Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default);

    public abstract Task WaitCompletionAsync();

    public Subscription<MqttMessage> SubscribeMessageObserver(IObserver<MqttMessage> observer) => publishObservers.Subscribe(observer);

    protected void OnMessageReceived(MqttMessage message)
    {
        try
        {
            MessageReceived?.Invoke(this, message);
        }
#pragma warning disable CA1031
        catch { }
#pragma warning restore CA1031

        publishObservers.Notify(message);
    }

    protected void OnConnected(ConnectedEventArgs args) => Connected?.Invoke(this, args);

    protected void OnDisconnected(DisconnectedEventArgs args) => Disconnected?.Invoke(this, args);

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        publishObservers.Dispose();
        return base.DisposeAsync();
    }
}