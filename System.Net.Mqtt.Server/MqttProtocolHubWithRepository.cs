using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using System.Security.Authentication;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.String;

namespace System.Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<T> : MqttProtocolHub, ISessionStateRepository<T>, IAsyncDisposable
    where T : MqttServerSessionState
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly ILogger logger;
    private readonly ChannelReader<Message> messageQueueReader;
    private readonly ChannelWriter<Message> messageQueueWriter;
    private readonly CancelableOperationScope messageWorker;
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
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var message = await messageQueueReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                statesEnumerator.Reset();
                while (statesEnumerator.MoveNext())
                {
                    Dispatch(statesEnumerator.Current.Value, message);
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

        if (!sessionState.TopicMatches(topic, out var maxQoS))
        {
            return;
        }

        var adjustedQoS = Math.Min(qos, maxQoS);

        LogOutgoingMessage(sessionState.ClientId, topic, payload.Length, adjustedQoS, false);

        sessionState.OutgoingWriter.TryWrite(qos == adjustedQoS ? message : message with { QoSLevel = adjustedQoS });
    }

    [LoggerMessage(17, LogLevel.Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage")]
    private partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        try
        {
            messageQueueWriter.Complete();
            await messageWorker.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Parallel.ForEach(states, state => (state.Value as IDisposable)?.Dispose());
            statesEnumerator.Dispose();
        }
    }

    #endregion

    #region Overrides of MqttProtocolHub

    public override async Task<MqttServerSession> AcceptConnectionAsync(NetworkTransport transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var reader = transport.Reader;

        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);
        var buffer = packet.Buffer;

        try
        {
            if (ConnectPacket.TryRead(in buffer, out var connPack, out _))
            {
                await ValidateAsync(transport, connPack, cancellationToken).ConfigureAwait(false);

                if (authHandler?.Authenticate(connPack.UserName, connPack.Password) == false)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, NotAuthorized }, cancellationToken).ConfigureAwait(false);
                    throw new AuthenticationException();
                }

                Message? willMessage = !IsNullOrEmpty(connPack.WillTopic)
                    ? new(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                return CreateSession(connPack, willMessage, transport, subscribeObserver, messageObserver);
            }
            else
            {
                throw new MissingConnectPacketException();
            }
        }
        finally
        {
            reader.AdvanceTo(buffer.End);
        }
    }

    protected abstract ValueTask ValidateAsync(NetworkTransport transport, ConnectPacket connectPacket, CancellationToken cancellationToken);

    protected abstract MqttServerSession CreateSession(ConnectPacket connectPacket, Message? willMessage, NetworkTransport transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver);

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