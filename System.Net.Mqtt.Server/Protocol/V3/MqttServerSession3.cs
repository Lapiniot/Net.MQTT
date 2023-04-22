using System.Net.Mqtt.Packets.V3;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState3> repository;
    private readonly IObserver<SubscribeMessage> subscribeObserver;
    private readonly IObserver<UnsubscribeMessage> unsubscribeObserver;
    private readonly IObserver<PacketRxMessage> packetRxObserver;
    private readonly IObserver<PacketTxMessage> packetTxObserver;
    private MqttServerSessionState3? sessionState;
    private CancellationTokenSource? globalCts;
    private Task? messageWorker;
    private Task? pingWorker;
    private PubRelDispatchHandler? resendPubRelHandler;
    private PublishDispatchHandler? resendPublishHandler;

    public MqttServerSession3(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState3> stateRepository, ILogger logger,
        Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, logger,
            observers is not null ? observers.IncomingMessage : throw new ArgumentNullException(nameof(observers)),
            false, maxUnflushedBytes)
    {
        repository = stateRepository;
        (subscribeObserver, unsubscribeObserver, _, packetRxObserver, packetTxObserver) = observers;
    }

    public bool CleanSession { get; init; }
    public ushort KeepAlive { get; init; }
    public Message? WillMessage { get; init; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        sessionState = repository.GetOrCreate(ClientId, CleanSession, out var existed);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        Post(new ConnAckPacket(ConnAckPacket.Accepted, existed));

        sessionState.IsActive = true;

        sessionState.WillMessage = WillMessage;

        globalCts = new();
        var stoppingToken = globalCts.Token;

        if (KeepAlive > 0)
        {
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), stoppingToken);
        }

        messageWorker = RunMessagePublisherAsync(stoppingToken);

        if (existed)
        {
            sessionState.DispatchPendingMessages(resendPubRelHandler ??= ResendPubRel, resendPublishHandler ??= ResendPublish);
        }
    }

    private void ResendPublish(ushort id, byte flags, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload) =>
        PostPublish(flags, id, topic, in payload);

    private void ResendPubRel(ushort id) => Post(PacketFlags.PubRelPacketMask | id);

    protected override async Task StoppingAsync()
    {
        try
        {
            if (sessionState!.WillMessage.HasValue)
            {
                OnMessageReceived(sessionState.WillMessage.Value);
                sessionState.WillMessage = null;
            }

            globalCts!.Cancel();

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
            sessionState!.IsActive = false;

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
            case CONNECT: break;
            case PUBLISH: OnPublish(flags, in reminder); break;
            case PUBACK: OnPubAck(in reminder); break;
            case PUBREC: OnPubRec(in reminder); break;
            case PUBREL: OnPubRel(in reminder); break;
            case PUBCOMP: OnPubComp(in reminder); break;
            case SUBSCRIBE: OnSubscribe(in reminder); break;
            case UNSUBSCRIBE: OnUnsubscribe(in reminder); break;
            case PINGREQ: OnPingReq(); break;
            case DISCONNECT: OnDisconnect(); break;
            default: MqttPacketHelpers.ThrowUnexpectedType((byte)type); break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPingReq() => Post(PacketFlags.PingRespPacket);

    private void OnDisconnect()
    {
        // Graceful disconnection: no need to dispatch last will message
        sessionState!.WillMessage = null;

        DisconnectReceived = true;

        StopAsync();
    }

    protected sealed override void OnPacketReceived(byte packetType, int totalLength)
    {
        DisconnectPending = false;
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateReceivedPacketMetrics(packetType, totalLength);
            packetRxObserver.OnNext(new(packetType, totalLength));
        }
    }

    protected sealed override void OnPacketSent(byte packetType, int totalLength)
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
        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}