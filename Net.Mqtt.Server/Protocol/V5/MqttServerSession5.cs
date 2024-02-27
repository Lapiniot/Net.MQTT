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

    public MqttServerSession5(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, int maxUnflushedBytes, ushort maxInFlight, int maxReceivePacketSize) :
        base(clientId, transport, logger, true)
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
        }.Write(Transport.Output, int.MaxValue);
        await Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

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
        catch (InvalidTopicAliasException) { }
        catch (ReceiveMaximumExceededException) { }
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

    protected override async Task WaitCompleteAsync()
    {
        try
        {
            await base.WaitCompleteAsync().ConfigureAwait(false);
        }
        catch (InvalidTopicAliasException)
        {
            Disconnect(DisconnectReason.TopicAliasInvalid);
        }
        catch (ReceiveMaximumExceededException)
        {
            Disconnect(DisconnectReason.ReceiveMaximumExceeded);
        }
        finally
        {
            Abort();
            // Ensure outgoing data stream producer is done, 
            // so there is no interference with direct Transport.Output writing operation
            await ProducerCompletion.ConfigureAwait(SuppressThrowing);

            if (!DisconnectReceived && DisconnectReason is not DisconnectReason.Normal)
            {
                await SendDisconnectAsync((byte)DisconnectReason).ConfigureAwait(false);
            }
        }

        async Task SendDisconnectAsync(byte reasonCode)
        {
            new DisconnectPacket(reasonCode).Write(Transport.Output, int.MaxValue);
            await Transport.Output.CompleteAsync().ConfigureAwait(false);
            await Transport.OutputCompletion!.ConfigureAwait(false);
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
            inflightSentinel.TryRelease();
        }
    }

    public override string ToString() => $"'{ClientId}' over '{Transport}' (MQTT5.0)";
}