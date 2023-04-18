using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<TSessionState, TConnPacket> : MqttProtocolHub,
    ISessionStateRepository<TSessionState>,
    ISessionStatisticsFeature,
    IAsyncDisposable
    where TSessionState : MqttServerSessionState
    where TConnPacket : MqttPacket, IBinaryReader<TConnPacket>
{
    private readonly ILogger logger;
    private readonly TimeSpan connectTimeout;
    private readonly ChannelReader<Message> messageQueueReader;
    private readonly ChannelWriter<Message> messageQueueWriter;

#pragma warning disable CA2213
    private readonly CancelableOperationScope messageWorker;
#pragma warning restore CA2213
    private readonly ConcurrentDictionary<string, TSessionState> states;
    private readonly IEnumerator<KeyValuePair<string, TSessionState>> statesEnumerator;
    private int disposed;

    protected MqttProtocolHubWithRepository(ILogger logger, TimeSpan connectTimeout)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
        this.connectTimeout = connectTimeout;

        states = new();
        statesEnumerator = states.GetEnumerator();
        (messageQueueReader, messageQueueWriter) = Channel.CreateUnbounded<Message>(new() { SingleReader = false, SingleWriter = false });
        messageWorker = CancelableOperationScope.Start(ProcessMessageQueueAsync);
    }

    protected ILogger Logger => logger;

    private async Task ProcessMessageQueueAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await messageQueueReader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (messageQueueReader.TryRead(out var message))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    statesEnumerator.Reset();
                    while (statesEnumerator.MoveNext())
                    {
                        Dispatch(statesEnumerator.Current.Value, message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (ChannelClosedException)
        {
            // expected
        }
    }

    private void Dispatch(MqttServerSessionState sessionState, Message message)
    {
        var (topic, payload, qos, _) = message;

        if (!sessionState.IsActive && qos == 0)
        {
            // Skip all incoming QoS 0 if session is inactive
            return;
        }

        if (!sessionState.TopicMatches(topic.Span, out var maxQoS))
        {
            return;
        }

        var adjustedQoS = Math.Min(qos, maxQoS);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            LogOutgoingMessage(sessionState.ClientId, UTF8.GetString(topic.Span), payload.Length, adjustedQoS, false);
        }

        sessionState.OutgoingWriter.TryWrite(qos == adjustedQoS ? message : message with { QoSLevel = adjustedQoS });
    }

    [LoggerMessage(17, LogLevel.Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage", SkipEnabledCheck = true)]
    private partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (statesEnumerator)
        {
            try
            {
                await using (messageWorker.ConfigureAwait(false))
                {
                    messageQueueWriter.Complete();
                }
            }
            finally
            {
                Parallel.ForEach(states, state => (state.Value as IDisposable)?.Dispose());
            }
        }
    }

    #endregion

    #region Overrides of MqttProtocolHub

    public sealed override async Task<MqttServerSession> AcceptConnectionAsync(NetworkTransportPipe transport,
        [NotNull] Observers observers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var reader = transport.Input;

        using var timeoutCts = new CancellationTokenSource(connectTimeout);
        using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, jointCts.Token).ConfigureAwait(false);
        var buffer = packet.Buffer;

        try
        {
            if (TConnPacket.TryRead(in buffer, out var connPacket, out var packetSize))
            {
                var (exception, connAckPacket) = Validate(connPacket);

                if (exception is null)
                {
                    return CreateSession(connPacket, transport, observers);
                }
                else
                {
                    // Negative acknowledgment is performed by the hub itself
                    await transport.Output.WriteAsync(connAckPacket, cancellationToken).ConfigureAwait(false);
                    // Mark output as completed, since no more data will be sent
                    // and wait output worker to complete, ensuring all data is flushed to the network
                    await transport.CompleteOutputAsync().ConfigureAwait(false);
                    // Notify observers directly about Rx/Tx activity, because 
                    // session will not be created at all due to the protocol error
                    observers.PacketRx?.OnNext(new((byte)PacketType.Connect, packetSize));
                    observers.PacketTx?.OnNext(new((byte)PacketType.ConnAck, connAckPacket.Length));
                    throw exception;
                }
            }
            else
            {
                MissingConnectPacketException.Throw();
                return null;
            }
        }
        finally
        {
            reader.AdvanceTo(buffer.Start);
        }
    }

    protected abstract (Exception? Exception, ReadOnlyMemory<byte> ConnAckPacket) Validate(TConnPacket? connPacket);

    protected abstract MqttServerSession CreateSession(TConnPacket connectPacket, NetworkTransportPipe transport, Observers observers);

    public sealed override void DispatchMessage(Message message) => messageQueueWriter.TryWrite(message);

    #endregion

    #region Implementation of ISessionStateRepository<out T>

    public TSessionState GetOrCreate(string clientId, bool clean, out bool existed)
    {
        TSessionState? current, created;

        if (clean)
        {
            created = CreateState(clientId, true);

            while (true)
            {
                current = states.GetOrAdd(clientId, created);
                if (created == current)
                {
                    existed = false;
                    break;
                }
                else if (states.TryUpdate(clientId, created, current))
                {
                    (current as IDisposable)?.Dispose();
                    existed = true;
                    break;
                }
            }

            return created;
        }

        if (states.TryGetValue(clientId, out current))
        {
            existed = true;
            return current;
        }

        created = CreateState(clientId, false);
        current = states.GetOrAdd(clientId, created);

        if (current == created)
        {
            existed = false;
        }
        else
        {
            (created as IDisposable)?.Dispose();
            existed = true;
        }

        return current;
    }

    protected abstract TSessionState CreateState(string clientId, bool clean);

    public void Remove(string clientId)
    {
        states.TryRemove(clientId, out var state);
        (state as IDisposable)?.Dispose();
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
            if (state.IsActive) total++;
        }

        return total;
    }

    #endregion
}