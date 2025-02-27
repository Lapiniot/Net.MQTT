using System.Collections.Concurrent;

namespace Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<TMessage, TSessionState, TConnPacket, TState> : MqttProtocolHub<MqttSessionState, TMessage>,
    ISessionStateRepository<TSessionState>,
    ISessionStatisticsFeature,
    IAsyncDisposable
    where TMessage : IApplicationMessage
    where TSessionState : MqttServerSessionState<TMessage, TState>
    where TConnPacket : IBinaryReader<TConnPacket>
{
    private readonly ILogger logger;
    private readonly ChannelReader<(MqttSessionState, TMessage)> messageQueueReader;
    private readonly ChannelWriter<(MqttSessionState, TMessage)> messageQueueWriter;

    private readonly Task messageWorker;
    private readonly ConcurrentDictionary<string, StateContext> states;
    private readonly IEnumerator<KeyValuePair<string, StateContext>> statesEnumerator;
    private int disposed;

    protected MqttProtocolHubWithRepository(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;

        states = new();
        statesEnumerator = states.GetEnumerator();
        (messageQueueReader, messageQueueWriter) = Channel.CreateUnbounded<(MqttSessionState, TMessage)>(new() { SingleReader = false, SingleWriter = false });
        messageWorker = ProcessMessageQueueAsync();
    }

    protected ILogger Logger => logger;
    public required IObserver<PacketRxMessage> PacketRxObserver { get; init; }
    public required IObserver<PacketTxMessage> PacketTxObserver { get; init; }

    private async Task ProcessMessageQueueAsync()
    {
        while (await messageQueueReader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (messageQueueReader.TryRead(out var message))
            {
                statesEnumerator.Reset();
                while (statesEnumerator.MoveNext())
                {
                    Dispatch(statesEnumerator.Current.Value.State, message);
                }
            }
        }
    }

    protected abstract void Dispatch(TSessionState sessionState, (MqttSessionState Sender, TMessage Message) message);

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (statesEnumerator)
        {
            messageQueueWriter.Complete();
            await messageWorker.ConfigureAwait(SuppressThrowing);
            Parallel.ForEach(states, state => (state.Value as IDisposable)?.Dispose());
        }
    }

    #endregion

    #region Overrides of MqttProtocolHub

    public sealed override async Task<MqttServerSession> AcceptConnectionAsync(TransportConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var reader = connection.Input;

        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);
        var buffer = packet.Buffer;
        reader.AdvanceTo(consumed: buffer.Start, examined: buffer.Start);

        if (TConnPacket.TryRead(in buffer, out var connPacket, out var packetSize))
        {
            var (exception, connAckPacket) = Validate(connPacket);

            if (exception is null)
            {
                return CreateSession(connPacket, connection);
            }
            else
            {
                // Negative acknowledgment is performed by the hub itself
                await connection.Output.WriteAsync(connAckPacket, cancellationToken).ConfigureAwait(false);
                // Mark output as completed, since no more data will be sent
                // and wait output worker to complete, ensuring all data is flushed to the network
                await connection.CompleteOutputAsync().ConfigureAwait(false);
                // Notify observers directly about Rx/Tx activity, because 
                // session will not be created at all due to the protocol error
                PacketRxObserver.OnNext(new(PacketType.CONNECT, packetSize));
                PacketTxObserver.OnNext(new(PacketType.CONNACK, connAckPacket.Length));
                throw exception;
            }
        }
        else
        {
            MissingConnectPacketException.Throw();
            return null;
        }
    }

    protected abstract (Exception? Exception, ReadOnlyMemory<byte> ConnAckPacket) Validate(TConnPacket connPacket);

    protected abstract MqttServerSession CreateSession(TConnPacket connectPacket, TransportConnection connection);

    public sealed override void DispatchMessage(MqttSessionState sender, TMessage message) => messageQueueWriter.TryWrite((sender, message));

    #endregion

    #region Implementation of ISessionStateRepository<out T>

    public TSessionState Acquire(string clientId, bool clean, out bool exists)
    {
        if (clean)
        {
            exists = false;
            return states.AddOrUpdate(clientId,
                addValueFactory: static (_, arg) => arg,
                updateValueFactory: static (_, existing, arg) =>
                {
                    existing.PendingTimer?.Dispose();
                    (existing.State as IDisposable)?.Dispose();
                    return arg;
                },
                factoryArgument: new StateContext(CreateState(clientId), null)).State;
        }

        if (states.TryGetValue(clientId, out var ctx) && states.TryUpdate(clientId, ctx with { PendingTimer = null }, ctx))
        {
            ctx.PendingTimer?.Dispose();
            exists = true;
            return ctx.State;
        }

        var created = CreateState(clientId);
        ctx = states.AddOrUpdate(clientId,
            addValueFactory: static (_, arg) => arg,
            updateValueFactory: static (_, existing, _) =>
            {
                existing.PendingTimer?.Dispose();
                return existing with { PendingTimer = null };
            },
            factoryArgument: new StateContext(created, null));

        if (ctx.State == created)
        {
            exists = false;
        }
        else
        {
            (created as IDisposable)?.Dispose();
            exists = true;
        }

        return ctx.State;
    }

    protected abstract TSessionState CreateState(string clientId);

    public void Discard(string clientId)
    {
        if (states.TryRemove(clientId, out var ctx))
        {
            using (ctx.State as IDisposable)
            using (ctx.PendingTimer) { }
        }
    }

    public void Release(string clientId, TimeSpan discardInactiveAfter)
    {
        if (states.TryGetValue(clientId, out var ctx))
        {
            ctx.State.IsActive = false;
            ctx.State.Trim();
            if (discardInactiveAfter != Timeout.InfiniteTimeSpan)
                DiscardDelayedAsync(ctx, discardInactiveAfter).Observe();
        }
    }

    private async Task DiscardDelayedAsync(StateContext ctx, TimeSpan delay)
    {
        using var timer = new PeriodicTimer(delay);
        var updated = ctx with { PendingTimer = timer };
        var clientId = ctx.State.ClientId;
        if (states.TryUpdate(clientId, updated, ctx))
        {
            if (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                if (states.TryRemove(new(clientId, updated)))
                {
                    if (ctx.State is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }
    }

    #endregion

    #region ISessionStatisticsFeature implementation

    public int GetTotalSessions()
    {
        var total = 0;
        foreach (var (_, state) in states)
        {
            total++;
        }

        return total;
    }

    public int GetActiveSessions()
    {
        var total = 0;
        foreach (var (_, state) in states)
        {
            if (state.State.IsActive) total++;
        }

        return total;
    }

    #endregion

    private sealed record StateContext(TSessionState State, PeriodicTimer? PendingTimer);
}