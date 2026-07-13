using System.Collections.Concurrent;

namespace Net.Mqtt.Client;

public abstract class MqttClient : MqttSession
{
    private readonly ObserversContainer<MqttMessage> messageObservers;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<ReadOnlyMemory<byte>>> pendingCompletions;
    private volatile int pendingCount;
    private volatile TaskCompletionSource? pendingTcs;
    private TaskCompletionSource? connAckTcs;
    private readonly bool disposeConnection;

    protected MqttClient(TransportConnection connection, bool disposeConnection, string? clientId) :
#pragma warning disable CA2000 // Dispose objects before losing scope
        base(connection)
#pragma warning restore CA2000 // Dispose objects before losing scope
    {
        messageObservers = new();
        pendingCompletions = new();
        ClientId = clientId;
        this.disposeConnection = disposeConnection;
    }

    public event EventHandler<ConnectedEventArgs>? Connected;
    public event EventHandler<DisconnectedEventArgs>? Disconnected;
#pragma warning disable CA1003 // Use generic event handler instances
    public event MessageReceivedHandler<MqttMessage>? MessageReceived;
#pragma warning restore CA1003 // Use generic event handler instances

    public string? ClientId { get; protected set; }

    protected bool ConnectionAcknowledged { get; private set; }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        pendingCount = 0;
        pendingTcs = null;
        ConnectionAcknowledged = false;
        connAckTcs?.TrySetCanceled(default);
        connAckTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return base.StartingAsync(cancellationToken);
    }

    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

    public virtual Task DisconnectAsync() => StopActivityAsync();

    public abstract Task<ReadOnlyMemory<byte>> SubscribeAsync((string topic, QoSLevel qos)[] filters,
        CancellationToken cancellationToken = default);

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
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (pendingCount is not 0)
        {
            if (pendingTcs is null)
            {
                Interlocked.CompareExchange(ref pendingTcs, new(TaskCreationOptions.RunContinuationsAsynchronously), null);
            }

            if (pendingCount is not 0)
            {
                return pendingTcs.Task.WaitAsync(cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    public Subscription<MqttMessage> SubscribeMessageObserver(IObserver<MqttMessage> observer) => messageObservers.Subscribe(observer);

    protected void OnMessageReceived(ref readonly MqttMessage message)
    {
        try
        {
            MessageReceived?.Invoke(this, new MqttMessageArgs<MqttMessage>(in message));
        }
#pragma warning disable CA1031
        catch { }
#pragma warning restore CA1031

        messageObservers.Notify(in message);
    }

    protected void OnConnected(ConnectedEventArgs args) => Connected?.Invoke(this, args);

    protected void OnDisconnected(bool graceful)
    {
        Disconnected?.Invoke(this, new DisconnectedEventArgs(graceful));

        if (graceful)
        {
            messageObservers.NotifyCompleted();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        Abort();
        messageObservers.Dispose();

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (disposeConnection)
            {
                await Connection.DisposeAsync().AsTask().ConfigureAwait(SuppressThrowing);
            }
        }
    }

    protected void OnMessageDeliveryStarted() => Interlocked.Increment(ref pendingCount);

    protected void OnMessageDeliveryComplete()
    {
        if (Interlocked.Decrement(ref pendingCount) is 0)
        {
            pendingTcs?.TrySetResult();
        }
    }

    protected void OnConnectionAcknowledged()
    {
        connAckTcs!.SetResult();
        ConnectionAcknowledged = true;
    }

    protected void OnConnectionAcknowledgeFailed(Exception exception)
    {
        connAckTcs!.SetException(exception);
    }

    protected Task WaitConnectionAcknowledgedAsync(CancellationToken cancellationToken)
    {
        return connAckTcs!.Task.WaitAsync(cancellationToken);
    }

    protected override Task OnConnectionClosedAsync()
    {
        // Cancel all pending completions
        Parallel.ForEach(pendingCompletions, tcs => tcs.Value.TrySetCanceled(Aborted));
        pendingCompletions.Clear();

        OnDisconnected(DisconnectReason is DisconnectReason.Normal);

        return Task.CompletedTask;
    }

    public override string? ToString() => ClientId ?? base.ToString();

    /// <summary>
    /// Acquires a cookie for tracking packet acknowledgment state.
    /// </summary>
    /// <param name="packetId">The packet ID for which to start tracking.</param>
    /// <returns>The acknowledgment state cookie.</returns>
    /// <remarks>
    /// Callers must ensure that the packet ID is unique within the scope of the client connection.
    /// Current acknowledgment state can be tracked by awaiting on the returned <see cref="AckCookie.Completion"/>
    /// task instance which remains in the pending state until <see cref="AcknowledgePacket(ushort, ReadOnlyMemory{byte})" />
    /// gets called with a corresponding <paramref name="packetId"/>, thus transiting the acknowledge state
    /// completion task to the complete state. The returned cookie must be disposed when it is no longer needed.
    /// </remarks>
    protected AckCookie AcquirePacketAcknowledgementCookie(ushort packetId) => AckCookie.Create(this, packetId);

    /// <summary>
    /// Acknowledges a packet with the specified ID by setting previously 
    /// acquired tracking cookie <see cref="AckCookie.Completion"/> state to completed.
    /// </summary>
    /// <param name="packetId">The packet ID to acknowledge.</param>
    /// <param name="result">The resulting data of the acknowledgment.</param>
    /// <exception cref="InvalidOperationException">Thrown when the packet is not tracked.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AcknowledgePacket(ushort packetId, ReadOnlyMemory<byte> result)
    {
        if (pendingCompletions.TryRemove(packetId, out var tcs))
        {
            tcs.SetResult(result);
        }
        else
        {
            ThrowAcknowledgmentException();
        }
    }

    [DoesNotReturn]
    private static void ThrowAcknowledgmentException()
    {
        throw new InvalidOperationException("Failed to acknowledge packet. Packet is not tracked.");
    }

    protected readonly record struct AckCookie : IDisposable
    {
        private readonly TaskCompletionSource<ReadOnlyMemory<byte>> tcs;
        private readonly MqttClient owner;
        private readonly ushort packetId;

        private AckCookie(MqttClient owner, ushort packetId, TaskCompletionSource<ReadOnlyMemory<byte>> tcs)
        {
            this.owner = owner;
            this.packetId = packetId;
            this.tcs = tcs;
        }

        public Task<ReadOnlyMemory<byte>> Completion => tcs.Task;

        internal static AckCookie Create(MqttClient owner, ushort packetId)
        {
            var tcs = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (owner.pendingCompletions.TryAdd(packetId, tcs))
            {
                return new AckCookie(owner, packetId, tcs);
            }

            tcs.SetResult(default);
            ThrowPacketIsAlreadyTracked();
            return default;
        }

        [DoesNotReturn]
        private static void ThrowPacketIsAlreadyTracked()
        {
            throw new InvalidOperationException("Failed to acquire packet acknowledgment cookie. Packet is already tracked.");
        }

        public void Dispose()
        {
            if (this.tcs.Task.IsCompleted)
            {
                return;
            }

            if (owner.pendingCompletions.TryRemove(packetId, out var tcs))
            {
                tcs.SetCanceled(default);
            }
        }
    }
}