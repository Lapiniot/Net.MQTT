using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Policies;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Channels.Channel;
using static System.Threading.Interlocked;
using static System.Threading.Tasks.TaskContinuationOptions;
using static System.TimeSpan;

namespace System.Net.Mqtt.Client;

public abstract partial class MqttClient : MqttClientProtocol, IConnectedObject
{
    private const long StateDisconnected = 0;
    private const long StateConnected = 1;
    private const long StateAborted = 2;
    private readonly IRetryPolicy reconnectPolicy;
    private readonly ClientSessionStateRepository repository;
    private readonly string clientId;
    private MqttClientSessionState sessionState;
    private long connectionState;
    private MqttConnectionOptions connectionOptions;
    private TaskCompletionSource connAckTcs;
    private PubRelDispatchHandler pubRelDispatchHandler;
    private PublishDispatchHandler publishDispatchHandler;

    protected MqttClient(NetworkTransport transport, string clientId, ClientSessionStateRepository repository,
        IRetryPolicy reconnectPolicy, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        this.clientId = clientId;
        this.repository = repository ?? new DefaultClientSessionStateRepository();
        this.reconnectPolicy = reconnectPolicy;

        (incomingQueueReader, incomingQueueWriter) = CreateUnbounded<MqttMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        dispatcher = new WorkerLoop(DispatchMessageAsync);
        publishObservers = new ObserversContainer<MqttMessage>();
        pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();
    }

    public TimeSpan ConnectTimeout { get; set; } = FromSeconds(5);

    public string ClientId => clientId;

    public bool CleanSession { get; private set; }

    protected bool ConnectionAcknowledged { get; private set; }

    public event EventHandler<ConnectedEventArgs> Connected;

    public event EventHandler<DisconnectedEventArgs> Disconnected;

    protected override void OnPacketReceived(byte packetType, int totalLength) { }

    protected override void OnConnAck(byte header, ReadOnlySequence<byte> reminder)
    {
        try
        {
            if(!ConnAckPacket.TryReadPayload(in reminder, out var packet))
            {
                throw new InvalidDataException(InvalidConnAckPacket);
            }

            packet.EnsureSuccessStatusCode();

            CleanSession = !packet.SessionPresent;

            sessionState = repository.GetOrCreate(clientId, CleanSession, out _);

            if(CleanSession)
            {
                // discard all not delivered application level messages
                while(incomingQueueReader.TryRead(out _)) { }
            }
            else
            {
                sessionState.DispatchPendingMessages(
                    pubRelDispatchHandler ??= new PubRelDispatchHandler(ResendPubRelPacket),
                    publishDispatchHandler ??= new PublishDispatchHandler(ResendPublishPacket));
            }

            _ = dispatcher.RunAsync(default);

            if(connectionOptions.KeepAlive > 0)
            {
                pinger = CancelableOperationScope.Start(StartPingerAsync);
            }

            connectionState = StateConnected;

            ConnectionAcknowledged = true;

            connAckTcs.TrySetResult();
        }
        catch(Exception e)
        {
            connAckTcs.TrySetException(e);
            throw;
        }

        Connected?.Invoke(this, ConnectedEventArgs.GetInstance(CleanSession));
    }

    private void ResendPublishPacket(ushort id, byte flags, string topic, in ReadOnlyMemory<byte> payload)
    {
        PostPublish(flags, id, topic, in payload);
    }

    private void ResendPubRelPacket(ushort id)
    {
        PostRaw(PacketFlags.PubRelPacketMask | id);
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        ConnectionAcknowledged = false;

        connAckTcs?.TrySetCanceled(default);
        connAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        _ = StartReconnectGuardAsync(Transport.Completion).ContinueWith(task =>
        {
            if(task.Exception is not null) { /* TODO: track somehow */ }
        }, default, NotOnRanToCompletion, TaskScheduler.Default);

        var cleanSession = Read(ref connectionState) != StateAborted && connectionOptions.CleanSession;

        var connectPacket = new ConnectPacket(clientId, ProtocolLevel, ProtocolName, connectionOptions.KeepAlive, cleanSession,
            connectionOptions.UserName, connectionOptions.Password, connectionOptions.LastWillTopic, connectionOptions.LastWillMessage,
            connectionOptions.LastWillQoS, connectionOptions.LastWillRetain);

        Post(connectPacket);
    }

    private async Task StartReconnectGuardAsync(Task completion)
    {
        try
        {
            await completion.ConfigureAwait(false);
        }
        catch
        {
            if(CompareExchange(ref connectionState, StateAborted, StateConnected) == StateConnected)
            {
                await StopActivityAsync().ConfigureAwait(false);
                var args = new DisconnectedEventArgs(true, reconnectPolicy != null);
                Disconnected?.Invoke(this, args);

                if(!args.TryReconnect || reconnectPolicy is null)
                {
                    throw;
                }

                await reconnectPolicy.RetryAsync(async token =>
                {
                    connectionOptions = connectionOptions with { CleanSession = false };
                    using var cts = new CancellationTokenSource(ConnectTimeout);
                    await StartActivityAsync(cts.Token).ConfigureAwait(false);
                    return false;
                }).ConfigureAwait(false);
            }
        }
    }

    protected async ValueTask WaitConnAckAsync(CancellationToken cancellationToken)
    {
        if(!connAckTcs.Task.IsCompleted)
        {
            await connAckTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override async Task StoppingAsync()
    {
        Parallel.ForEach(pendingCompletions.Values, CancelCompletion);
        pendingCompletions.Clear();

        if(pinger is not null)
        {
            await pinger.DisposeAsync().ConfigureAwait(false);
            pinger = null;
        }

        await dispatcher.StopAsync().ConfigureAwait(false);

        await base.StoppingAsync().ConfigureAwait(false);

        var graceful = CompareExchange(ref connectionState, StateDisconnected, StateConnected) == StateConnected;

        if(graceful)
        {
            if(CleanSession) repository.Remove(clientId);

            await Transport.SendAsync(new byte[] { 0b1110_0000, 0 }, default).ConfigureAwait(false);
        }

        await Transport.DisconnectAsync().ConfigureAwait(false);

        if(graceful)
        {
            Disconnected?.Invoke(this, new DisconnectedEventArgs(false, false));
        }
    }

    private void CancelCompletion(TaskCompletionSource<object> source)
    {
        source.TrySetCanceled();
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using(sessionState)
        using(publishObservers)
        await using(dispatcher.ConfigureAwait(false))
        {
            try
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                if(pinger is not null)
                {
                    await pinger.DisposeAsync().ConfigureAwait(false);
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
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return ConnectAsync(new MqttConnectionOptions(), cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task DisconnectAsync()
    {
        return StopActivityAsync();
    }

    #endregion
}