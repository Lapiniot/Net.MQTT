using Net.Mqtt.Packets.V3;
using static Net.Mqtt.PacketType;

namespace Net.Mqtt.Client;

public abstract partial class MqttClient3Core : MqttClient
{
    private const long StateDisconnected = 0;
    private const long StateConnected = 1;
    private const long StateAborted = 2;
    private readonly IRetryPolicy reconnectPolicy;
    private readonly int maxInFlight;
    private MqttConnectionOptions3 connectionOptions;
    private long connectionState;
    private MqttSessionState<PublishDeliveryState> sessionState;
    private AsyncSemaphore inflightSentinel;

    protected MqttClient3Core(NetworkConnection connection, bool disposeConnection, string clientId, int maxInFlight,
        IRetryPolicy reconnectPolicy, byte protocolLevel, string protocolName) :
        base(connection, disposeConnection, clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(protocolName);

        this.reconnectPolicy = reconnectPolicy;
        this.maxInFlight = maxInFlight;
        pendingCompletions = new();
        connectionOptions = MqttConnectionOptions3.Default;
        ProtocolLevel = protocolLevel;
        ProtocolName = protocolName;
    }

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool CleanSession { get; private set; }

    protected byte ProtocolLevel { get; }

    protected string ProtocolName { get; }

    protected sealed override void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder)
    {
        var type = (PacketType)(header >>> 4);
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incurring constant number values ordered in the following way
        switch (type)
        {
            case CONNACK: OnConnAck(in reminder); break;
            case PUBLISH: OnPublish(header, in reminder); break;
            case PUBACK: OnPubAck(in reminder); break;
            case PUBREC: OnPubRec(in reminder); break;
            case PUBREL: OnPubRel(in reminder); break;
            case PUBCOMP: OnPubComp(in reminder); break;
            case SUBACK: OnSubAck(in reminder); break;
            case UNSUBACK: OnUnsubAck(in reminder); break;
            case PINGRESP: break;
            default: ProtocolErrorException.Throw((byte)type); break;
        }
    }

    private void OnConnAck(in ReadOnlySequence<byte> reminder)
    {
        try
        {
            if (!ConnAckPacket.TryReadPayload(in reminder, out var packet))
            {
                MalformedPacketException.Throw("CONNACK");
            }

            packet.EnsureSuccessStatusCode();

            CleanSession = !packet.SessionPresent;

            if (CleanSession || sessionState is null)
            {
                sessionState = new();
            }

            if (!CleanSession)
            {
                foreach (var (id, state) in sessionState.PublishState)
                {
                    ResendPublishPacket(id, state);
                }
            }

            if (connectionOptions.KeepAlive > 0)
            {
                pingWorker = StartPingWorkerAsync(Aborted);
            }

            connectionState = StateConnected;

            OnConnAckSuccess();
        }
        catch (Exception e)
        {
            OnConnAckError(e);
            throw;
        }

        OnConnected(ConnectedEventArgs.GetInstance(CleanSession));
    }

    private void ResendPublishPacket(ushort id, PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
            PostPublish((byte)(state.Flags | PacketFlags.Duplicate), id, state.Topic, state.Payload);
        else
            Post(PacketFlags.PubRelPacketMask | id);

        OnMessageDeliveryStarted();
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

    public sealed override Task ConnectAsync(CancellationToken cancellationToken = default) => ConnectAsync(MqttConnectionOptions3.Default, cancellationToken);

    public async Task ConnectAsync(MqttConnectionOptions3 options, CancellationToken cancellationToken = default)
    {
        await ConnectCoreAsync(options, cancellationToken).ConfigureAwait(false);
        await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
    }

    protected Task ConnectCoreAsync(MqttConnectionOptions3 options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        connectionOptions = options;
        return StartActivityAsync(cancellationToken);
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<PacketDispatchBlock>(new() { SingleReader = true, SingleWriter = false });
        inflightSentinel = new(maxInFlight, maxInFlight);

        await Connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        Transport.Reset();
        Transport.Start();
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        StartReconnectGuardAsync(Transport.InputCompletion).Observe();

        var cleanSession = Volatile.Read(ref connectionState) != StateAborted && connectionOptions.CleanSession;

        var connectPacket = new ConnectPacket(ToUtf8String(ClientId), ProtocolLevel,
            ToUtf8String(ProtocolName), connectionOptions.KeepAlive, cleanSession,
            ToUtf8String(connectionOptions.UserName), ToUtf8String(connectionOptions.Password),
            ToUtf8String(connectionOptions.LastWillTopic), connectionOptions.LastWillMessage,
            connectionOptions.LastWillQoS, connectionOptions.LastWillRetain);

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
                OnDisconnected(args);

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

        Parallel.ForEach(pendingCompletions, static pair => pair.Value.TrySetCanceled());
        pendingCompletions.Clear();

        Abort();

        if (pingWorker is not null)
        {
            await pingWorker.ConfigureAwait(SuppressThrowing);
            pingWorker = null;
        }

        await base.StoppingAsync().ConfigureAwait(false);

        await DisconnectCoreAsync(gracefull: Interlocked.CompareExchange(
            ref connectionState, StateDisconnected, StateConnected) == StateConnected).ConfigureAwait(false);
    }
}