using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession : Server.MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState> repository;
    private readonly IObserver<SubscribeMessage> subscribeObserver;
    private readonly IObserver<UnsubscribeMessage> unsubscribeObserver;
    private readonly IObserver<PacketRxMessage> packetRxObserver;
    private readonly IObserver<PacketTxMessage> packetTxObserver;
    private MqttServerSessionState sessionState;
    private CancellationTokenSource globalCts;
    private Task messageWorker;
    private Task pingWorker;
    private bool disconnectPending;
    private PubRelDispatchHandler resendPubRelHandler;
    private PublishDispatchHandler resendPublishHandler;
    private int disposed;

    public MqttServerSession(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState> stateRepository, ILogger logger,
        Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, logger, observers?.IncomingMessage, false, maxUnflushedBytes)
    {
        repository = stateRepository;
        (subscribeObserver, unsubscribeObserver, _, packetRxObserver, packetTxObserver) = observers;
    }

    public bool CleanSession { get; init; }
    public ushort KeepAlive { get; init; }
    public Message? WillMessage { get; init; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        sessionState = repository.GetOrCreate(ClientId, CleanSession, out var existing);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        sessionState.IsActive = true;

        sessionState.WillMessage = WillMessage;

        globalCts = new();
        var stoppingToken = globalCts.Token;

        if (KeepAlive > 0)
        {
            disconnectPending = true;
            pingWorker = RunPingMonitorAsync(stoppingToken);
        }

        messageWorker = RunMessagePublisherAsync(stoppingToken);

        if (existing)
        {
            sessionState.DispatchPendingMessages(resendPubRelHandler ??= ResendPubRel, resendPublishHandler ??= ResendPublish);
        }
    }

    private void ResendPublish(ushort id, byte flags, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload) =>
        PostPublish(flags, id, topic, in payload);

    private void ResendPubRel(ushort id) => Post(PacketFlags.PubRelPacketMask | id);

    private async Task RunPingMonitorAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(KeepAlive * 1.5));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (Volatile.Read(ref disconnectPending))
            {
                StopAsync().Observe();
                break;
            }

            disconnectPending = true;
        }
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            if (sessionState.WillMessage.HasValue)
            {
                OnMessageReceived(sessionState.WillMessage.Value);
                sessionState.WillMessage = null;
            }

            globalCts.Cancel();

            using (globalCts)
            {
                try
                {
                    if (pingWorker is not null)
                    {
                        try
                        {
                            await pingWorker.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            pingWorker = null;
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (messageWorker is not null)
                        {
                            try
                            {
                                await messageWorker.ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { }
                            finally
                            {
                                messageWorker = null;
                            }
                        }
                    }
                    finally
                    {
                        await base.StoppingAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        catch (ConnectionClosedException)
        {
            // Expected here - shouldn't cause exception during termination even 
            // if connection was aborted before due to any reasons
        }
        finally
        {
            sessionState.IsActive = false;

            if (CleanSession)
            {
                repository.Remove(ClientId);
            }
            else
            {
                sessionState.Trim();
            }
        }
    }

    protected sealed override void Dispatch(PacketType type, byte flags, in ReadOnlySequence<byte> reminder)
    {
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incuring constant number values ordered in the following way
        switch (type)
        {
            case Publish: OnPublish(flags, in reminder); break;
            case PubAck: OnPubAck(flags, in reminder); break;
            case PubRec: OnPubRec(flags, in reminder); break;
            case PubRel: OnPubRel(flags, in reminder); break;
            case PubComp: OnPubComp(flags, in reminder); break;
            case Subscribe: OnSubscribe(flags, in reminder); break;
            case Unsubscribe: OnUnsubscribe(flags, in reminder); break;
            case PingReq: OnPingReq(flags, in reminder); break;
            case Disconnect: OnDisconnect(flags, in reminder); break;
            default: MqttPacketHelpers.ThrowUnexpectedType((byte)type); break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnPingReq(byte header, in ReadOnlySequence<byte> reminder) => Post(PacketFlags.PingRespPacket);

    protected void OnDisconnect(byte header, in ReadOnlySequence<byte> reminder)
    {
        // Graceful disconnection: no need to dispatch last will message
        sessionState.WillMessage = null;

        DisconnectReceived = true;

        StopAsync();
    }

    protected internal sealed override void OnPacketReceived(byte packetType, int totalLength)
    {
        disconnectPending = false;
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateReceivedPacketMetrics(packetType, totalLength);
            packetRxObserver.OnNext(new(packetType, totalLength));
        }
    }

    protected internal sealed override void OnPacketSent(byte packetType, int totalLength)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateSentPacketMetrics(packetType, totalLength);
            packetTxObserver.OnNext(new(packetType, totalLength));
        }
    }

    partial void UpdateReceivedPacketMetrics(byte packetType, int packetSize);

    partial void UpdateSentPacketMetrics(byte packetType, int packetSize);

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}