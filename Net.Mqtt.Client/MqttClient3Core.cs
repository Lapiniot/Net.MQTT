using System.Collections.Concurrent;
using System.Diagnostics;
using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Client;

public abstract partial class MqttClient3Core : MqttClient
{
    private MqttConnectionOptions3 connectionOptions;
    private MqttSessionState<PublishDeliveryState>? sessionState;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object?>> pendingCompletions;

    protected MqttClient3Core(TransportConnection connection, bool disposeConnection, string? clientId,
        int maxInFlight, byte protocolLevel, string protocolName) :
        base(connection, disposeConnection, clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(protocolName);

        this.maxInFlight = maxInFlight;
        pendingCompletions = new();
        connectionOptions = MqttConnectionOptions3.Default;
        ProtocolLevel = protocolLevel;
        ProtocolName = protocolName;
        inflightSentinel = new(0);
    }

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool CleanSession { get; private set; }

    protected byte ProtocolLevel { get; }

    protected string ProtocolName { get; }

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
        if (!inflightSentinel.TryReset(maxInFlight, maxInFlight))
        {
            Debug.Assert(false, "There shouldn't be any pending async. waiters at this stage. Check logic correctness!");
            inflightSentinel = new(maxInFlight, maxInFlight);
        }

        var cleanSession = DisconnectReason is DisconnectReason.Normal && connectionOptions.CleanSession;
        DisconnectReason = DisconnectReason.Normal;

        await Connection.StartAsync(cancellationToken).ConfigureAwait(false);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        var connectPacket = new ConnectPacket(ToUtf8String(ClientId), ProtocolLevel,
            ToUtf8String(ProtocolName), connectionOptions.KeepAlive, cleanSession,
            ToUtf8String(connectionOptions.UserName), ToUtf8String(connectionOptions.Password),
            ToUtf8String(connectionOptions.LastWillTopic), connectionOptions.LastWillMessage,
            connectionOptions.LastWillQoS, connectionOptions.LastWillRetain);

        Post(connectPacket);

        static ReadOnlyMemory<byte> ToUtf8String(string? value) => value is not (null or "") ? UTF8.GetBytes(value) : ReadOnlyMemory<byte>.Empty;
    }

    protected override async Task OnConnectionClosingAsync(CancellationToken cancellationToken)
    {
        if (pingWorker is not null)
        {
            await pingWorker.ConfigureAwait(SuppressThrowing);
            pingWorker = null;
        }

        if (DisconnectReason is DisconnectReason.Normal)
        {
            await Connection.Output.WriteAsync((byte[])[0b1110_0000, 0], cancellationToken).ConfigureAwait(false);
        }
    }

    protected override async Task OnConnectionClosedAsync()
    {
        // Cancel all pending completions
        Parallel.ForEach(pendingCompletions, tcs => tcs.Value.TrySetCanceled(Aborted));
        pendingCompletions.Clear();

        // Cancel all potential leftovers (there might be pending descriptors with completion sources in the queue, 
        // but producer loop was already terminated due to other reasons, like cancellation via cancellationToken)
        await Parallel.ForEachAsync(reader!.ReadAllAsync(), (descriptor, _) =>
        {
            descriptor.Completion?.TrySetCanceled(Aborted);
            return default;
        }).ConfigureAwait(SuppressThrowing);

        OnDisconnected(new(DisconnectReason is DisconnectReason.Normal));
    }
}