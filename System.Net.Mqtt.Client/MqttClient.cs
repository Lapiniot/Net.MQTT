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

public abstract partial class MqttClient : MqttClientProtocol, ISessionStateRepository<MqttClientSessionState>, IConnectedObject
{
    private const long StateDisconnected = 0;
    private const long StateConnected = 1;
    private const long StateAborted = 2;
    private readonly IRetryPolicy reconnectPolicy;
    private readonly ISessionStateRepository<MqttClientSessionState> repository;
    private readonly string clientId;
#pragma warning disable CA2213 // False positive from roslyn analyzer
    private DelayWorkerLoop pinger;
#pragma warning restore CA2213
    private MqttClientSessionState sessionState;
    private long connectionState;
    private MqttConnectionOptions connectionOptions;
    private TaskCompletionSource connAckTcs;

    protected MqttClient(NetworkTransport transport, string clientId,
        ISessionStateRepository<MqttClientSessionState> repository,
        IRetryPolicy reconnectPolicy) :
        base(transport)
    {
        this.clientId = clientId;
        this.repository = repository ?? this;
        this.reconnectPolicy = reconnectPolicy;

        (incomingQueueReader, incomingQueueWriter) = CreateUnbounded<MqttMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        dispatcher = new WorkerLoop(DispatchMessageAsync);
        publishObservers = new ObserversContainer<MqttMessage>();
        pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();
    }

    public string ClientId => clientId;

    public bool CleanSession { get; private set; }

    protected bool ConnectionAcknowledged { get; private set; }

    public event EventHandler<ConnectedEventArgs> Connected;

    public event EventHandler<DisconnectedEventArgs> Disconnected;

    protected override void OnPacketReceived() { }

    protected override void OnConnAck(byte header, ReadOnlySequence<byte> reminder)
    {
        try
        {
            if(!ConnAckPacket.TryReadPayload(reminder, out var packet))
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
                Parallel.ForEach(sessionState.ResendPackets, Post);
            }

            _ = dispatcher.RunAsync(default);

            if(connectionOptions.KeepAlive > 0)
            {
                pinger = new DelayWorkerLoop(PingAsync, FromSeconds(connectionOptions.KeepAlive));
                _ = pinger.RunAsync(default);
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

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        ConnectionAcknowledged = false;
        connAckTcs = new TaskCompletionSource(null, TaskCreationOptions.RunContinuationsAsynchronously);

        await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        var cleanSession = Read(ref connectionState) != StateAborted && connectionOptions.CleanSession;

        var connectPacket = new ConnectPacket(clientId, ProtocolLevel, ProtocolName, connectionOptions.KeepAlive, cleanSession,
            connectionOptions.UserName, connectionOptions.Password, connectionOptions.LastWillTopic, connectionOptions.LastWillMessage,
            connectionOptions.LastWillQoS, connectionOptions.LastWillRetain);

        Post(connectPacket);
    }

    protected async Task WaitConnAckAsync(CancellationToken cancellationToken)
    {
        await connAckTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task StoppingAsync()
    {
        Parallel.ForEach(pendingCompletions.Values, CancelCompletion);
        pendingCompletions.Clear();

        if(pinger != null)
        {
            await using(pinger.ConfigureAwait(false))
            {
                await pinger.StopAsync().ConfigureAwait(false);
            }

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

    protected override bool OnCompleted(Exception exception = null)
    {
        if(exception != null && CompareExchange(ref connectionState, StateAborted, StateConnected) == StateConnected)
        {
            StopActivityAsync().ContinueWith(t =>
            {
                var args = new DisconnectedEventArgs(true, reconnectPolicy != null);

                Disconnected?.Invoke(this, args);

                if(args.TryReconnect)
                {
                    reconnectPolicy?.RetryAsync(async token =>
                    {
                        await StartActivityAsync(token).ConfigureAwait(false);
                        return true;
                    });
                }
            }, default, RunContinuationsAsynchronously, TaskScheduler.Default);
        }

        return true;
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

    #region Implementation of ISessionStateRepository<out SessionState>

    public MqttClientSessionState GetOrCreate(string clientId, bool cleanSession, out bool existingSession)
    {
        if(cleanSession) Remove(clientId);
        existingSession = sessionState != null;
        return sessionState ?? new MqttClientSessionState();
    }

    public void Remove(string clientId)
    {
        sessionState?.Dispose();
        sessionState = null;
    }

    #endregion

    #region Implementation of IConnectedObject

    public bool IsConnected => IsRunning;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        connectionOptions = new MqttConnectionOptions();
        return StartActivityAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task DisconnectAsync()
    {
        return StopActivityAsync();
    }

    #endregion
}