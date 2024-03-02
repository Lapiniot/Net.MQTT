using System.Collections.Concurrent;
using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public sealed partial class MqttClient5 : MqttClient
{
    private ChannelReader<PacketDescriptor> reader;
    private ChannelWriter<PacketDescriptor> writer;
    private readonly NetworkConnection connection;
    private MqttConnectionOptions5 connectionOptions;
    private Task pingWorker;
    private MqttSessionState<Message> sessionState;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;
    private readonly ObserversContainer<MqttMessage5> message5Observers;

    public MqttClient5(NetworkConnection connection, bool disposeConnection, string clientId, int maxInFlight) :
        base(connection, disposeConnection, clientId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxInFlight, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxInFlight, ushort.MaxValue);

        this.connection = connection;
        this.maxInFlight = maxInFlight;
        connectionOptions = MqttConnectionOptions5.Default;
        pendingCompletions = new();
        message5Observers = new();
        serverAliases = new();
        clientAliases = new();
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<PacketDescriptor>(new() { SingleReader = true, SingleWriter = false });
        receivedIncompleteQoS2 = 0;
        ReceiveMaximum = connectionOptions.ReceiveMaximum;
        MaxReceivePacketSize = connectionOptions.MaxPacketSize;
        MaxSendPacketSize = int.MaxValue;
        ServerTopicAliasMaximum = 0;
        DisconnectReceived = false;
        DisconnectReason = DisconnectReason.Normal;
        serverAliases.Initialize(connectionOptions.TopicAliasMaximum);
        clientAliases.Initialize(0);

        await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        Transport.Reset();
        Transport.Start();

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        var connPacket = new ConnectPacket(ClientId is { } ? UTF8.GetBytes(ClientId) : default,
            keepAlive: connectionOptions.KeepAlive, cleanStart: connectionOptions.CleanStart,
            connectionOptions is { UserName: { } uname } ? UTF8.GetBytes(uname) : default,
            connectionOptions is { Password: { } pwd } ? UTF8.GetBytes(pwd) : default,
            connectionOptions is { LastWillTopic: { } lw } ? UTF8.GetBytes(lw) : default,
            connectionOptions.LastWillMessage, connectionOptions.LastWillQoS, connectionOptions.LastWillRetain)
        {
            ReceiveMaximum = ReceiveMaximum,
            MaximumPacketSize = (uint)MaxReceivePacketSize,
            TopicAliasMaximum = connectionOptions.TopicAliasMaximum,
            SessionExpiryInterval = connectionOptions.SessionExpiryInterval,
            AuthenticationData = connectionOptions.AuthenticationData,
            AuthenticationMethod = connectionOptions.AuthenticationMethod,
            RequestProblem = connectionOptions.RequestProblem,
            RequestResponse = connectionOptions.RequestResponse,
            UserProperties = connectionOptions.UserProperties,
            WillContentType = connectionOptions.WillContentType,
            WillDelayInterval = connectionOptions.WillDelayInterval,
            WillExpiryInterval = connectionOptions.WillExpiryInterval,
            WillCorrelationData = connectionOptions.WillCorrelationData,
            WillPayloadFormat = connectionOptions.WillPayloadFormat,
            WillResponseTopic = connectionOptions.WillResponseTopic,
            WillUserProperties = connectionOptions.WillUserProperties
        };

        Post(connPacket);

        StartDisconnectMonitorAsync().Observe();
    }

    protected override async Task StoppingAsync()
    {
        Abort();

        if (pingWorker is not null)
        {
            await pingWorker.ConfigureAwait(SuppressThrowing);
            pingWorker = null;
        }

        await base.StoppingAsync().ConfigureAwait(false);

        CancelPendingCompletions();

        if (!DisconnectReceived)
        {
            try
            {
                new DisconnectPacket((byte)DisconnectReason).Write(Transport.Output, int.MaxValue);
                await Transport.Output.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                // Mark output channel as completed and wait until all data is flushed to the network 
                await Transport.CompleteOutputAsync().ConfigureAwait(SuppressThrowing);
            }
        }

        await DisconnectCoreAsync(DisconnectReason is DisconnectReason.Normal).ConfigureAwait(SuppressThrowing);
    }

    private void CancelPendingCompletions()
    {
        writer!.Complete();

        Parallel.ForEach(pendingCompletions, c => c.Value.TrySetCanceled(Aborted));
        pendingCompletions.Clear();

        // Cancel all potential leftovers (there might be pending descriptors with completion sources in the queue, 
        // but producer loop was already terminated due to other reasons, like cancellation via cancellationToken)
        while (reader!.TryRead(out var descriptor))
        {
            descriptor.Completion?.TrySetCanceled(Aborted);
        }
    }

    private async Task StartDisconnectMonitorAsync()
    {
        await RunDisconnectWatcherAsync(ConsumerCompletion, ProducerCompletion).ConfigureAwait(SuppressThrowing);
        await StopActivityAsync().ConfigureAwait(SuppressThrowing);
    }

    public override ValueTask DisposeAsync()
    {
        message5Observers.Dispose();
        return base.DisposeAsync();
    }

    private async Task RunPingWorkerAsync(TimeSpan period, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            Post(PacketFlags.PingReqPacket);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AcknowledgePacket(ushort packetId, object result = null)
    {
        if (pendingCompletions.TryGetValue(packetId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CompleteMessageDelivery(ushort id)
    {
        if (sessionState.DiscardMessageDeliveryState(id))
        {
            OnMessageDeliveryComplete();
            inflightSentinel.TryRelease(1);
        }
    }

    public override Task ConnectAsync(CancellationToken cancellationToken = default) =>
        ConnectAsync(MqttConnectionOptions5.Default, true, cancellationToken);

    public async Task ConnectAsync(MqttConnectionOptions5 options, bool waitAcknowledgement = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        connectionOptions = options;

        await StartActivityAsync(cancellationToken).ConfigureAwait(false);

        if (waitAcknowledgement)
        {
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Subscription<MqttMessage5> SubscribeMessageObserver(IObserver<MqttMessage5> observer) =>
        message5Observers.Subscribe(observer);
}