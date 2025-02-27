using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Server.Protocol.V5;

public sealed partial class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
#pragma warning disable CA2213
    private MqttServerSessionState5? state;
#pragma warning restore CA2213

    public bool CleanStart { get; init; }
    public uint ExpiryInterval { get; init; }
    public Message5? WillMessage { get; init; }
    public uint WillDelayInterval { get; init; }
    public bool HasAssignedClientId { get; init; }

    public MqttServerSession5(string clientId, TransportConnection connection,
        ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, int maxUnflushedBytes, ushort maxInFlight, int maxReceivePacketSize) :
        base(clientId, connection, logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxInFlight, 1);
        this.maxUnflushedBytes = maxUnflushedBytes;
        this.stateRepository = stateRepository;
        clientAliases = new();
        serverAliases = new();
        inflightSentinel = new(maxInFlight, maxInFlight);
        MaxReceivePacketSize = maxReceivePacketSize;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = stateRepository.Acquire(ClientId, CleanStart, out var exists);
        clientAliases.Initialize(aliasMaximum: TopicAliasMaximum);
        serverAliases.Initialize(aliasMaximum: ClientTopicAliasMaximum);

        new ConnAckPacket(ConnAckPacket.Accepted, exists)
        {
            SharedSubscriptionAvailable = false,
            TopicAliasMaximum = TopicAliasMaximum,
            ReceiveMaximum = ReceiveMaximum,
            MaximumPacketSize = (uint)MaxReceivePacketSize,
            AssignedClientId = HasAssignedClientId ? UTF8.GetBytes(ClientId) : ReadOnlyMemory<byte>.Empty,
        }.Write(Connection.Output, int.MaxValue);
        await Connection.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

        state.SetWillMessageState(WillMessage, IncomingObserver);
        (reader, writer) = Channel.CreateUnbounded<PacketDescriptor>(new() { SingleReader = true, SingleWriter = false });
        receivedIncompleteQoS2 = 0;

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        state.IsActive = true;
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            state!.PublishWillMessage(TimeSpan.FromSeconds(WillDelayInterval));
            await base.StoppingAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (!DisconnectReceived && DisconnectReason is not DisconnectReason.Normal)
                {
                    try
                    {
                        new DisconnectPacket((byte)DisconnectReason).Write(Connection.Output, int.MaxValue);
                        await Connection.Output.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        await Connection.Output.CompleteAsync().ConfigureAwait(false);
                        await Connection.Completion.ConfigureAwait(SuppressThrowing);
                    }
                }
            }
            finally
            {
                if (ExpiryInterval is 0)
                {
                    stateRepository.Discard(ClientId);
                }
                else
                {
                    stateRepository.Release(ClientId, ExpiryInterval is uint.MaxValue ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(ExpiryInterval));
                }
            }
        }
    }

    protected override async Task RunDisconnectWatcherAsync(Task[] tasksToWatch)
    {
        try
        {
            await base.RunDisconnectWatcherAsync(tasksToWatch).ConfigureAwait(false);
        }
        catch (InvalidTopicAliasException)
        {
            Disconnect(DisconnectReason.TopicAliasInvalid);
        }
        catch (ReceiveMaximumExceededException)
        {
            Disconnect(DisconnectReason.ReceiveMaximumExceeded);
        }
    }

    private void OnPacketReceived(PacketType packetType, int totalLength)
    {
        DisconnectPending = false;

        if (RuntimeOptions.MetricsCollectionSupported)
        {
            UpdateReceivedPacketMetrics(packetType, totalLength);
            PacketRxObserver.OnNext(new(packetType, totalLength));
        }
    }

    private void OnPacketSent(PacketType packetType, int totalLength)
    {
        if (RuntimeOptions.MetricsCollectionSupported)
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
            inflightSentinel.TryRelease();
        }
    }

    public override string ToString() => $"'{ClientId}' over '{Connection}' (MQTT5.0)";
}