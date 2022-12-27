using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<T> : MqttProtocolHub, ISessionStateRepository<T>, IAsyncDisposable
    where T : MqttServerSessionState
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly ILogger logger;
    private readonly ChannelReader<Message> messageQueueReader;
    private readonly ChannelWriter<Message> messageQueueWriter;

#pragma warning disable CA2213
    private readonly CancelableOperationScope messageWorker;
#pragma warning restore CA2213
    private readonly ConcurrentDictionary<string, T> states;
    private readonly IEnumerator<KeyValuePair<string, T>> statesEnumerator;
    private int disposed;

    protected MqttProtocolHubWithRepository(ILogger logger, IMqttAuthenticationHandler authHandler)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
        this.authHandler = authHandler;

        states = new();
        statesEnumerator = states.GetEnumerator();
        (messageQueueReader, messageQueueWriter) = Channel.CreateUnbounded<Message>(new() { SingleReader = false, SingleWriter = false });
        messageWorker = CancelableOperationScope.Start(ProcessMessageQueueAsync);
    }

    protected ILogger Logger => logger;

    private async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await messageQueueReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (messageQueueReader.TryRead(out var message))
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver,
        IObserver<PacketRxMessage> packetRxObserver, IObserver<PacketTxMessage> packetTxObserver,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var reader = transport.Input;

        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);
        var buffer = packet.Buffer;

        try
        {
            if (ConnectPacket.TryRead(in buffer, out var connPack, out _))
            {
                await ValidateAsync(transport, connPack, cancellationToken).ConfigureAwait(false);

                if (authHandler?.Authenticate(UTF8.GetString(connPack.UserName.Span), UTF8.GetString(connPack.Password.Span)) == false)
                {
                    await transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.NotAuthorized }, cancellationToken).ConfigureAwait(false);
                    InvalidCredentialsException.Throw();
                }

                Message? willMessage = !connPack.WillTopic.IsEmpty
                    ? new(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                var session = CreateSession(connPack, willMessage, transport, subscribeObserver, messageObserver, packetRxObserver, packetTxObserver);
                session.OnPacketReceived(0b0001, (int)buffer.Length);
                return session;
            }
            else
            {
                MissingConnectPacketException.Throw();
                return null;
            }
        }
        finally
        {
            reader.AdvanceTo(buffer.End);
        }
    }

    protected abstract ValueTask ValidateAsync(NetworkTransportPipe transport, ConnectPacket connectPacket, CancellationToken cancellationToken);

    protected abstract MqttServerSession CreateSession(ConnectPacket connectPacket, Message? willMessage, NetworkTransportPipe transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver,
        IObserver<PacketRxMessage> packetRxObserver, IObserver<PacketTxMessage> packetTxObserver);

    public override void DispatchMessage(Message message)
    {
        if (states.IsEmpty) return; // TODO: this blocks states collection, consider removal after profiling
        messageQueueWriter.TryWrite(message);
    }

    #endregion

    #region Implementation of ISessionStateRepository<out T>

    public T GetOrCreate(string clientId, bool clean, out bool existed)
    {
        T current;

        if (clean)
        {
            var replacement = CreateState(clientId, true);

            while (!states.TryAdd(clientId, replacement))
            {
                while (states.TryGetValue(clientId, out current))
                {
                    if (!states.TryUpdate(clientId, replacement, current)) continue;
                    (current as IDisposable)?.Dispose();
                    existed = true;
                    return replacement;
                }
            }

            existed = false;
            return replacement;
        }

        while (!states.TryGetValue(clientId, out current))
        {
            var created = CreateState(clientId, false);
            if (states.TryAdd(clientId, created))
            {
                existed = false;
                return created;
            }

            (created as IDisposable)?.Dispose();
        }

        existed = true;
        return current;
    }

    protected abstract T CreateState(string clientId, bool clean);

    public void Remove(string clientId)
    {
        states.TryRemove(clientId, out var state);
        (state as IDisposable)?.Dispose();
    }

    #endregion
}