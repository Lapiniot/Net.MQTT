using System.Threading.Tasks.Sources;

namespace Net.Mqtt.Client;

#pragma warning disable CA1003
public delegate void MessageReceivedHandler(object sender, ref readonly MqttMessage message);

public abstract class MqttClient : MqttSession
{
    private readonly ObserversContainer<MqttMessage> publishObservers;
    private volatile int pendingCount;
    private volatile TaskCompletionSource pendingTcs;
    private readonly ManualResetValueTaskSource connAckMrvts;
    protected bool ConnectionAcknowledged { get; private set; }

    protected internal MqttClient(string clientId, NetworkTransportPipe transport, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        publishObservers = new();
        ClientId = clientId;
        connAckMrvts = new();
    }

    public event EventHandler<ConnectedEventArgs> Connected;
    public event EventHandler<DisconnectedEventArgs> Disconnected;
    public event MessageReceivedHandler MessageReceived;

    public string ClientId { get; protected set; }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        pendingCount = 0;
        pendingTcs = null;
        ConnectionAcknowledged = false;
        connAckMrvts.Reset();
        return base.StartingAsync(cancellationToken);
    }

    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

    public virtual Task DisconnectAsync() => StopActivityAsync();

    public abstract Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] filters, CancellationToken cancellationToken = default);

    public abstract Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default);

    public abstract Task PublishAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default);

    public Task PublishAsync(string topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default) =>
        PublishAsync(UTF8.GetBytes(topic), payload, qosLevel, retain, cancellationToken);

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when QoS1 and QoS2 message delivery counter reaches zero value.
    /// This effectively means there are no pending deliveries at the momment.
    /// </summary>
    /// <remarks>
    /// Call this method only once per connection session and after all 
    /// <see cref="PublishAsync(ReadOnlyMemory{byte}, ReadOnlyMemory{byte}, QoSLevel, bool, CancellationToken)"/> calls are completed.
    /// Otherwise consistent information about pending delivery progress is not guaranteed due to potential race condition.
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for external cancellation monitoring.</param>
    /// <returns><see cref="Task"/> that can be awaited asynchronously.</returns>
    public Task WaitMessageDeliveryCompleteAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (pendingCount is not 0)
        {
            if (pendingTcs is null)
                Interlocked.CompareExchange(ref pendingTcs, new(TaskCreationOptions.RunContinuationsAsynchronously), null);

            if (pendingCount is not 0)
                return pendingTcs.Task.WaitAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Subscription<MqttMessage> SubscribeMessageObserver(IObserver<MqttMessage> observer) => publishObservers.Subscribe(observer);

    protected void OnMessageReceived(ref readonly MqttMessage message)
    {
        try
        {
            MessageReceived?.Invoke(this, in message);
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

    protected void OnMessageDeliveryStarted() => Interlocked.Increment(ref pendingCount);

    protected void OnMessageDeliveryComplete()
    {
        if (Interlocked.Decrement(ref pendingCount) is 0)
            pendingTcs?.TrySetResult();
    }

    protected void OnConnAckSuccess()
    {
        connAckMrvts.SetResult();
        ConnectionAcknowledged = true;
    }

    protected void OnConnAckError(Exception exception) => connAckMrvts.SetException(exception);

    protected async ValueTask WaitConnAckReceivedAsync(CancellationToken cancellationToken) =>
        await connAckMrvts.ValueTask.AsTask().WaitAsync(cancellationToken).ConfigureAwait(false);

    private sealed class ManualResetValueTaskSource : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<int> mrvtsc = new() { RunContinuationsAsynchronously = true };

        public ValueTask ValueTask => new(this, mrvtsc.Version);

        #region IValueTaskSource implementation

        public void GetResult(short token) => mrvtsc.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => mrvtsc.GetStatus(token);
        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            mrvtsc.OnCompleted(continuation, state, token, flags);

        #endregion

        public void Reset() => mrvtsc.Reset();
        public void SetResult() => mrvtsc.SetResult(0);
        public void SetException(Exception exception) => mrvtsc.SetException(exception);
    }
}