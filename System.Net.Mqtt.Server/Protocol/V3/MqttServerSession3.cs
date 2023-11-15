using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState3> repository;
    private readonly int maxUnflushedBytes;
    private readonly AsyncSemaphoreLight inflightSentinel;
    private MqttServerSessionState3? state;
    private ChannelReader<PacketDescriptor>? reader;
    private ChannelWriter<PacketDescriptor>? writer;

    public MqttServerSession3(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState3> stateRepository,
        ILogger logger, int maxUnflushedBytes, ushort maxInFlight, int maxReceivePacketSize) :
        base(clientId, transport, logger, false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxInFlight, 1);
        this.maxUnflushedBytes = maxUnflushedBytes;
        repository = stateRepository;
        inflightSentinel = new(maxInFlight, maxInFlight);
        MaxReceivePacketSize = maxReceivePacketSize;
    }

    public bool CleanSession { get; init; }
    public Message3? WillMessage { get; init; }
    public required IObserver<SubscribeMessage3> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }
    public required IObserver<PacketRxMessage> PacketRxObserver { get; init; }
    public required IObserver<PacketTxMessage> PacketTxObserver { get; init; }
    public required IObserver<IncomingMessage3> IncomingObserver { get; init; }

    protected sealed override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = repository.Acquire(ClientId, CleanSession, out var exists);

        new ConnAckPacket(ConnAckPacket.Accepted, exists).Write(Transport.Output);
        await Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
        state.WillMessage = WillMessage;

        (reader, writer) = Channel.CreateUnbounded<PacketDescriptor>(new() { SingleReader = true, SingleWriter = false });
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        state.IsActive = true;
    }

    protected sealed override async Task StoppingAsync()
    {
        try
        {
            if (state!.WillMessage is { } willMessage)
            {
                IncomingObserver.OnNext(new(state, willMessage));
                state.WillMessage = null;
            }

            await base.StoppingAsync().ConfigureAwait(false);
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

    private void OnPacketReceived(PacketType packetType, int totalLength)
    {
        DisconnectPending = false;
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateReceivedPacketMetrics(packetType, totalLength);
            PacketRxObserver.OnNext(new(packetType, totalLength));
        }
    }

    private void OnPacketSent(PacketType packetType, int totalLength)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateSentPacketMetrics(packetType, totalLength);
            PacketTxObserver.OnNext(new(packetType, totalLength));
        }
    }

    partial void UpdateReceivedPacketMetrics(PacketType packetType, int packetSize);

    partial void UpdateSentPacketMetrics(PacketType packetType, int packetSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CompleteMessageDelivery(ushort id)
    {
        if (state!.DiscardMessageDeliveryState(id))
        {
            inflightSentinel!.TryRelease();
        }
    }

    public override string ToString() => $"'{ClientId}' over '{Transport}' (MQTT3.1)";
}