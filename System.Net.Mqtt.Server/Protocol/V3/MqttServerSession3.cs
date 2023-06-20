using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState3> repository;
    private readonly int maxUnflushedBytes;
    private MqttServerSessionState3? state;
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

        new ConnAckPacket(ConnAckPacket.Accepted, exists).Write(Transport.Output, out _);
        await Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        state.IsActive = true;

        state.WillMessage = WillMessage;

        if (KeepAlive > 0)
        {
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), Aborted);
        }

        messageWorker = RunMessagePublisherAsync(Aborted);

        if (exists)
        {
            state.DispatchPendingMessages(resendPublishHandler ??= ResendPublish);
        }
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

            Abort();
            try
            {
                try
                {
                    if (pingWorker is not null)
                    {
                        await pingWorker.ConfigureAwait(false);
                    }
                }
                finally
                {
                    await messageWorker!.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                await base.StoppingAsync().ConfigureAwait(false);
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
}