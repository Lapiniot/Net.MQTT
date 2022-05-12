using System.Policies;
using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Client;

public abstract partial class MqttClient : MqttClientProtocol, IConnectedObject
{
    private const long StateDisconnected = 0;
    private const long StateConnected = 1;
    private const long StateAborted = 2;
    private readonly string clientId;
    private readonly IRetryPolicy reconnectPolicy;
    private readonly ClientSessionStateRepository repository;
    private TaskCompletionSource connAckTcs;
    private MqttConnectionOptions connectionOptions;
    private long connectionState;
    private CancelableOperationScope messageNotifierScope;
    private PublishDispatchHandler publishDispatchHandler;
    private PubRelDispatchHandler pubRelDispatchHandler;
    private MqttClientSessionState sessionState;

    protected MqttClient(NetworkTransport transport, string clientId, ClientSessionStateRepository repository,
        IRetryPolicy reconnectPolicy, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this.clientId = clientId;
        this.repository = repository;
        this.reconnectPolicy = reconnectPolicy;

        (incomingQueueReader, incomingQueueWriter) = Channel.CreateUnbounded<MqttMessage>(new() { SingleReader = true, SingleWriter = true });
        publishObservers = new();
        pendingCompletions = new();
    }

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public string ClientId => clientId;

    public bool CleanSession { get; private set; }

    protected bool ConnectionAcknowledged { get; private set; }

    public event EventHandler<ConnectedEventArgs> Connected;

    public event EventHandler<DisconnectedEventArgs> Disconnected;

    protected sealed override void OnPacketReceived(byte packetType, int totalLength) { }

    protected sealed override void OnConnAck(byte header, ReadOnlySequence<byte> reminder)
    {
        try
        {
            if (!ConnAckPacket.TryReadPayload(in reminder, out var packet))
            {
                ThrowInvalidConnAckPacket();
            }

            packet.EnsureSuccessStatusCode();

            CleanSession = !packet.SessionPresent;

            sessionState = repository.GetOrCreate(clientId, CleanSession, out _);

            if (CleanSession)
            {
                // discard all not delivered application level messages
                while (incomingQueueReader.TryRead(out _)) { }
            }
            else
            {
                sessionState.DispatchPendingMessages(
                    pubRelDispatchHandler ??= ResendPubRelPacket,
                    publishDispatchHandler ??= ResendPublishPacket);
            }

            messageNotifierScope = CancelableOperationScope.Start(StartMessageNotifierAsync);

            if (connectionOptions.KeepAlive > 0)
            {
                pingScope = CancelableOperationScope.Start(StartPingWorkerAsync);
            }

            connectionState = StateConnected;

            ConnectionAcknowledged = true;

            connAckTcs.TrySetResult();
        }
        catch (Exception e)
        {
            connAckTcs.TrySetException(e);
            throw;
        }

        Connected?.Invoke(this, ConnectedEventArgs.GetInstance(CleanSession));
    }

    private void ResendPublishPacket(ushort id, byte flags, Utf8String topic, ReadOnlyMemory<byte> payload) => PostPublish(flags, id, topic, in payload);

    private void ResendPubRelPacket(ushort id) => Post(PacketFlags.PubRelPacketMask | id);

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        ConnectionAcknowledged = false;

        connAckTcs?.TrySetCanceled(default);
        connAckTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        _ = StartReconnectGuardAsync(Transport.Completion).ContinueWith(task =>
        {
            if (task.Exception is not null)
            {
                /* TODO: track somehow */
            }
        }, default, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);

        var cleanSession = Volatile.Read(ref connectionState) != StateAborted && connectionOptions.CleanSession;

        var connectPacket = new ConnectPacket(ToUtf8String(clientId), ProtocolLevel,
            ToUtf8String(ProtocolName), connectionOptions.KeepAlive, cleanSession,
            ToUtf8String(connectionOptions.UserName), ToUtf8String(connectionOptions.Password),
            ToUtf8String(connectionOptions.LastWillTopic), connectionOptions.LastWillMessage,
            (byte)connectionOptions.LastWillQoS, connectionOptions.LastWillRetain);

        Post(connectPacket);

        static ReadOnlyMemory<byte> ToUtf8String(string value) => value is not (null or "") ? UTF8.GetBytes(value) : ReadOnlyMemory<byte>.Empty;
    }

    private async Task StartReconnectGuardAsync(Task completion)
    {
        try
        {
            await completion.ConfigureAwait(false);
        }
        catch
        {
            if (Interlocked.CompareExchange(ref connectionState, StateAborted, StateConnected) == StateConnected)
            {
                await StopActivityAsync().ConfigureAwait(false);
                var args = new DisconnectedEventArgs(true, reconnectPolicy != null);
                Disconnected?.Invoke(this, args);

                if (!args.TryReconnect || reconnectPolicy is null)
                {
                    throw;
                }

                await reconnectPolicy.RetryAsync(async _ =>
                {
                    connectionOptions = connectionOptions with { CleanSession = false };
                    using var cts = new CancellationTokenSource(ConnectTimeout);
                    await StartActivityAsync(cts.Token).ConfigureAwait(false);
                    return false;
                }).ConfigureAwait(false);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Task WaitConnAckAsync(CancellationToken cancellationToken)
    {
        var task = connAckTcs.Task;
        return task.IsCompletedSuccessfully ? Task.CompletedTask : task.WaitAsync(cancellationToken);
    }

    public Task CompleteAsync() => sessionState.CompleteAsync();

    protected override async Task StoppingAsync()
    {
        Parallel.ForEach(pendingCompletions, static pair => pair.Value.TrySetCanceled());
        pendingCompletions.Clear();

        if (pingScope is not null)
        {
            await pingScope.DisposeAsync().ConfigureAwait(false);
            pingScope = null;
        }

        await messageNotifierScope.DisposeAsync().ConfigureAwait(false);

        await base.StoppingAsync().ConfigureAwait(false);

        var graceful = Interlocked.CompareExchange(ref connectionState, StateDisconnected, StateConnected) == StateConnected;

        if (graceful)
        {
            if (CleanSession) repository.Remove(clientId);

            await Transport.SendAsync(new byte[] { 0b1110_0000, 0 }, default).ConfigureAwait(false);
        }

        await Transport.DisconnectAsync().ConfigureAwait(false);

        if (graceful)
        {
            Disconnected?.Invoke(this, new(false, false));
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (publishObservers)
        {
            try
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    if (pingScope is not null)
                    {
                        await pingScope.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await messageNotifierScope.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    public Task ConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        connectionOptions = options;
        return StartActivityAsync(cancellationToken);
    }

    #region Implementation of IConnectedObject

    public bool IsConnected => IsRunning;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task ConnectAsync(CancellationToken cancellationToken = default) => ConnectAsync(new(), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task DisconnectAsync() => StopActivityAsync();

    #endregion
}