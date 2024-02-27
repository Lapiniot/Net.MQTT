using System.Collections.Concurrent;
using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public sealed partial class MqttClient5 : MqttClient
{
    private ChannelReader<PacketDescriptor> reader;
    private ChannelWriter<PacketDescriptor> writer;
    private readonly NetworkConnection connection;
    private MqttConnectionOptions5 connectionOptions;
    private Task pingCompletion;
    private MqttSessionState<Message> sessionState;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;
    private readonly ObserversContainer<MqttMessage5> message5Observers;

    public MqttClient5(NetworkConnection connection, string clientId, int maxInFlight, bool disposeTransport) :
#pragma warning disable CA2000
        base(clientId, new NetworkTransportPipe(connection), disposeTransport)
#pragma warning restore CA2000
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

    public ushort KeepAlive { get; private set; }

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
            TopicAliasMaximum = connectionOptions.TopicAliasMaximum
        };

        Post(connPacket);

        StartDisconnectMonitorAsync().Observe();
    }

    protected override async Task StoppingAsync()
    {
        writer.Complete();
        await ProducerCompletion.ConfigureAwait(SuppressThrowing);
        // Cancel all potential leftovers (there might be pending descriptors with completion sources in the queue, 
        // but producer loop was already terminated due to other reasons, like cancellation via cancellationToken)
        while (reader.TryRead(out var descriptor))
        {
            descriptor.Completion?.TrySetCanceled();
        }

        Parallel.ForEach(pendingCompletions, c => c.Value.TrySetCanceled());
        pendingCompletions.Clear();

        Abort();

        if (pingCompletion is not null)
        {
            await pingCompletion.ConfigureAwait(SuppressThrowing);
            pingCompletion = null;
        }

        await base.StoppingAsync().ConfigureAwait(false);

        try
        {
            if (!DisconnectReceived)
            {
                await Transport.Output.WriteAsync(new byte[] { 0b1110_0000, 0 }, default).ConfigureAwait(false);
                await Transport.CompleteOutputAsync().ConfigureAwait(SuppressThrowing);
            }
        }
        finally
        {
            await connection.DisconnectAsync().ConfigureAwait(SuppressThrowing);
            await Transport.StopAsync().ConfigureAwait(SuppressThrowing);
            OnDisconnected(new DisconnectedEventArgs(DisconnectReason is not DisconnectReason.Normal, true));
        }
    }

    private async Task StartDisconnectMonitorAsync()
    {
        await WaitCompleteAsync().ConfigureAwait(SuppressThrowing);
        await StopActivityAsync().ConfigureAwait(SuppressThrowing);
    }

    public override async ValueTask DisposeAsync()
    {
        message5Observers.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private async Task StartPingWorkerAsync(TimeSpan period, CancellationToken cancellationToken)
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