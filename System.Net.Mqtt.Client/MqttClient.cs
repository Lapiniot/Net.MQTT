using System.Net.Mqtt.Packets.V3;
using System.Policies;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Client;

public abstract partial class MqttClient : MqttClientSession, IConnectedObject
{
    private const long StateDisconnected = 0;
    private const long StateConnected = 1;
    private const long StateAborted = 2;
    private readonly string clientId;
    private readonly IRetryPolicy reconnectPolicy;
    private readonly NetworkConnection connection;
    private TaskCompletionSource connAckTcs;
    private MqttConnectionOptions connectionOptions;
    private long connectionState;
    private CancelableOperationScope messageNotifyScope;
    private MqttClientSessionState sessionState;
    private readonly AsyncSemaphore inflightSentinel;

    protected MqttClient(NetworkConnection connection, string clientId, int maxInFlight, IRetryPolicy reconnectPolicy, bool disposeTransport) :
#pragma warning disable CA2000
        base(new NetworkTransportPipe(connection), disposeTransport)
#pragma warning restore CA2000
    {
        this.clientId = clientId;
        this.reconnectPolicy = reconnectPolicy;
        this.connection = connection;

        (incomingQueueReader, incomingQueueWriter) = Channel.CreateUnbounded<MqttMessage>(new() { SingleReader = true, SingleWriter = true });
        publishObservers = new();
        pendingCompletions = new();
        inflightSentinel = new(maxInFlight, maxInFlight);
    }

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public string ClientId => clientId;

    public bool CleanSession { get; private set; }

    protected bool ConnectionAcknowledged { get; private set; }

    public event EventHandler<ConnectedEventArgs> Connected;

    public event EventHandler<DisconnectedEventArgs> Disconnected;

    protected sealed override void OnPacketReceived(byte packetType, int totalLength) { }

    protected sealed override void Dispatch(PacketType type, byte header, in ReadOnlySequence<byte> reminder)
    {
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incurring constant number values ordered in the following way
        switch (type)
        {
            case CONNACK: OnConnAck(header, in reminder); break;
            case PUBLISH: OnPublish(header, in reminder); break;
            case PUBACK: OnPubAck(header, in reminder); break;
            case PUBREC: OnPubRec(header, in reminder); break;
            case PUBREL: OnPubRel(header, in reminder); break;
            case PUBCOMP: OnPubComp(header, in reminder); break;
            case SUBACK: OnSubAck(header, in reminder); break;
            case UNSUBACK: OnUnsubAck(header, in reminder); break;
            case PINGRESP: OnPingResp(header, in reminder); break;
            default: ProtocolErrorException.Throw((byte)type); break;
        }
    }

    protected void OnConnAck(byte header, in ReadOnlySequence<byte> reminder)
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

            if (CleanSession)
            {
                // discard all not delivered application level messages
                while (incomingQueueReader.TryRead(out _)) { }
            }
            else
            {
                foreach (var (id, state) in sessionState.PublishState)
                {
                    ResendPublishPacket(id, state);
                }
            }

            messageNotifyScope = CancelableOperationScope.Start(StartMessageNotifierAsync);

            if (connectionOptions.KeepAlive > 0)
            {
                pingScope = CancelableOperationScope.Start(StartPingWorkerAsync);
            }

            connectionState = StateConnected;

            ConnectionAcknowledged = true;

            connAckTcs.TrySetResult();
        }
        catch (Exception e)
        {
            connAckTcs.TrySetException(e);
            throw;
        }

        Connected?.Invoke(this, ConnectedEventArgs.GetInstance(CleanSession));
    }

    private void ResendPublishPacket(ushort id, PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
            PostPublish(state.Flags, id, state.Topic, state.Payload);
        else
            Post(PacketFlags.PubRelPacketMask | id);
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        ConnectionAcknowledged = false;

        connAckTcs?.TrySetCanceled(default);
        connAckTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        Transport.Reset();
        Transport.Start();
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        StartReconnectGuardAsync(Transport.InputCompletion).Observe();

        var cleanSession = Volatile.Read(ref connectionState) != StateAborted && connectionOptions.CleanSession;

        var connectPacket = new ConnectPacket(ToUtf8String(clientId), ProtocolLevel,
            ToUtf8String(ProtocolName), connectionOptions.KeepAlive, cleanSession,
            ToUtf8String(connectionOptions.UserName), ToUtf8String(connectionOptions.Password),
            ToUtf8String(connectionOptions.LastWillTopic), connectionOptions.LastWillMessage,
            (byte)connectionOptions.LastWillQoS, connectionOptions.LastWillRetain);

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
                Disconnected?.Invoke(this, args);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Task WaitConnAckAsync(CancellationToken cancellationToken)
    {
        var task = connAckTcs.Task;
        return task.IsCompletedSuccessfully ? Task.CompletedTask : task.WaitAsync(cancellationToken);
    }

    public Task CompleteAsync() => sessionState.CompleteAsync();

    protected override async Task StoppingAsync()
    {
        Parallel.ForEach(pendingCompletions, static pair => pair.Value.TrySetCanceled());
        pendingCompletions.Clear();

        if (pingScope is not null)
        {
            await pingScope.DisposeAsync().ConfigureAwait(false);
            pingScope = null;
        }

        await messageNotifyScope.DisposeAsync().ConfigureAwait(false);

        await base.StoppingAsync().ConfigureAwait(false);

        var graceful = Interlocked.CompareExchange(ref connectionState, StateDisconnected, StateConnected) == StateConnected;

        try
        {
            if (graceful)
            {
                if (CleanSession) sessionState = null;

                await Transport.Output.WriteAsync(new byte[] { 0b1110_0000, 0 }, default).ConfigureAwait(false);
                await Transport.CompleteOutputAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await connection.DisconnectAsync().ConfigureAwait(false);
            await Transport.StopAsync().ConfigureAwait(false);
        }

        if (graceful)
        {
            Disconnected?.Invoke(this, new(false, false));
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (publishObservers)
        await using (connection.ConfigureAwait(false))
        await using (Transport.ConfigureAwait(false))
        {
            try
            {
                try
                {
                    if (pingScope is not null)
                    {
                        await pingScope.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await base.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (messageNotifyScope is not null)
                {
                    await messageNotifyScope.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    public Task ConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        connectionOptions = options;
        return StartActivityAsync(cancellationToken);
    }

    #region Implementation of IConnectedObject

    public bool IsConnected => IsRunning;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task ConnectAsync(CancellationToken cancellationToken = default) => ConnectAsync(new(), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task DisconnectAsync() => StopActivityAsync();

    #endregion
}