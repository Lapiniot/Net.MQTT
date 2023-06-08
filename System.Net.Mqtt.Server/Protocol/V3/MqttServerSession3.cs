using System.Net.Mqtt.Packets.V3;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState3> repository;
    private readonly int maxUnflushedBytes;
    private MqttServerSessionState3? state;
    private CancellationTokenSource? globalCts;
    private Task? messageWorker;
    private Task? pingWorker;
    private Action<ushort, PublishDeliveryState>? resendPublishHandler;
    private ChannelReader<DispatchBlock>? reader;
    private ChannelWriter<DispatchBlock>? writer;

    public MqttServerSession3(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState3> stateRepository,
        ILogger logger, int maxUnflushedBytes) :
        base(clientId, transport, logger, false)
    {
        this.maxUnflushedBytes = maxUnflushedBytes;
        repository = stateRepository;
    }

    public bool CleanSession { get; init; }
    public ushort KeepAlive { get; init; }
    public Message3? WillMessage { get; init; }
    public required IObserver<SubscribeMessage3> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }
    public required IObserver<PacketRxMessage> PacketRxObserver { get; init; }
    public required IObserver<PacketTxMessage> PacketTxObserver { get; init; }
    public required IObserver<IncomingMessage3> IncomingObserver { get; init; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = repository.Acquire(ClientId, CleanSession, out var exists);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        Post(new ConnAckPacket(ConnAckPacket.Accepted, exists));

        state.IsActive = true;

        state.WillMessage = WillMessage;

        globalCts = new();
        var stoppingToken = globalCts.Token;

        if (KeepAlive > 0)
        {
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), stoppingToken);
        }

        messageWorker = RunMessagePublisherAsync(stoppingToken);

        if (exists)
        {
            state.DispatchPendingMessages(resendPublishHandler ??= ResendPublish);
        }
    }

    private void ResendPublish(ushort id, PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
            PostPublish(state.Flags, id, state.Topic, state.Payload);
        else
            Post(PacketFlags.PubRelPacketMask | id);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            if (state!.WillMessage is { } willMessage)
            {
                IncomingObserver.OnNext(new(willMessage, state));
                state.WillMessage = null;
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
            if (CleanSession)
            {
                repository.Discard(ClientId);
            }
            else
            {
                repository.Release(ClientId, Timeout.InfiniteTimeSpan);
            }
        }
    }

    protected sealed override void Dispatch(PacketType type, byte header, in ReadOnlySequence<byte> reminder)
    {
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incurring constant number values ordered in the following way
        switch (type)
        {
            case CONNECT: break;
            case PUBLISH: OnPublish(header, in reminder); break;
            case PUBACK: OnPubAck(in reminder); break;
            case PUBREC: OnPubRec(in reminder); break;
            case PUBREL: OnPubRel(in reminder); break;
            case PUBCOMP: OnPubComp(in reminder); break;
            case SUBSCRIBE: OnSubscribe(header, in reminder); break;
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
        state!.WillMessage = null;
        DisconnectReceived = true;
        StopActivityAsync().Observe();
    }

    protected sealed override void OnPacketReceived(byte packetType, int totalLength)
    {
        DisconnectPending = false;
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateReceivedPacketMetrics(packetType, totalLength);
            PacketRxObserver.OnNext(new(packetType, totalLength));
        }
    }

    protected sealed override void OnPacketSent(byte packetType, int totalLength)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateSentPacketMetrics(packetType, totalLength);
            PacketTxObserver.OnNext(new(packetType, totalLength));
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

    private readonly record struct DispatchBlock(MqttPacket? Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Buffer, uint Raw);
}