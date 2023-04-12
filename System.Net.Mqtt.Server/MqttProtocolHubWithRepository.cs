using System.Collections.Concurrent;
using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<T> : MqttProtocolHub,
    ISessionStateRepository<T>,
    ISessionStatisticsFeature,
    IAsyncDisposable
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

        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);
        var buffer = packet.Buffer;

        try
        {
            if (ConnectPacket.TryRead(in buffer, out var connPack, out _))
            {
                try
                {
                    var acknowledge = AcknowledgeConnectionAsync;

                    await ValidateAsync(connPack, acknowledge, cancellationToken).ConfigureAwait(false);

                    if (authHandler?.Authenticate(UTF8.GetString(connPack.UserName.Span), UTF8.GetString(connPack.Password.Span)) == false)
                    {
                        await acknowledge(ConnAckPacket.CredentialsRejected, cancellationToken).ConfigureAwait(false);
                        InvalidCredentialsException.Throw();
                    }

                    var session = CreateSession(connPack, transport, observers);

                    await transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.Accepted }, cancellationToken).ConfigureAwait(false);

                    session.OnPacketReceived(0b0001, (int)buffer.Length);
                    session.OnPacketSent(0b0010, 4);

                    return session;
                }
                catch
                {
                    observers.PacketRx?.OnNext(new(0b0001, (int)buffer.Length));
                    throw;
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
            reader.AdvanceTo(buffer.End);
        }

        async Task AcknowledgeConnectionAsync(byte reasonCode, CancellationToken token)
        {
            await transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, 0, reasonCode }, token).ConfigureAwait(false);
            observers.PacketTx?.OnNext(new(0b0010, 4));
        }
    }

    protected static Message? BuildWillMessage([NotNull] ConnectPacket packet) =>
        !packet.WillTopic.IsEmpty
            ? new(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain)
            : null;

    protected abstract ValueTask ValidateAsync(ConnectPacket connectPacket, Func<byte, CancellationToken, Task> acknowledge, CancellationToken cancellationToken);

    protected abstract MqttServerSession CreateSession(ConnectPacket connectPacket, NetworkTransportPipe transport, Observers observers);

    public sealed override void DispatchMessage(Message message) => messageQueueWriter.TryWrite(message);

    #endregion

    #region Implementation of ISessionStateRepository<out T>

    public T GetOrCreate(string clientId, bool clean, out bool existed)
    {
        T current, created;

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

    protected abstract T CreateState(string clientId, bool clean);

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