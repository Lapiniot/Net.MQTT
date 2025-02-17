using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Net.Mqtt.Server.Protocol.V3;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Server;

public sealed partial class MqttServer : IMqttServer, IDisposable
{
    public const int Stopped = 0;
    public const int Running = 1;
    public const int Disposed = 2;

    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly ILogger<MqttServer> logger;
    private readonly ServerOptions options;
    private readonly IReadOnlyDictionary<string, Func<IAsyncEnumerable<TransportConnection>>> listenerFactories;
    private volatile TaskCompletionSource updateStatsSignal;
    private int state;
    private readonly ProtocolHub3? hub3;
    private readonly ProtocolHub4? hub4;
    private readonly ProtocolHub5? hub5;
    private readonly CancellationTokenSource globalCts;

    public MqttServer(ILogger<MqttServer> logger, ServerOptions options,
        IReadOnlyDictionary<string, Func<IAsyncEnumerable<TransportConnection>>> listenerFactories,
        IMeterFactory? meterFactory, IMqttAuthenticationHandler? authenticationHandler)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger;
        this.options = options;
        this.listenerFactories = listenerFactories;

        if (options.Protocols.HasFlag(MqttProtocol.Level3))
        {
            hub3 = new(logger, authenticationHandler, options)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        if (options.Protocols.HasFlag(MqttProtocol.Level4))
        {
            hub4 = new(logger, authenticationHandler, options)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        if (options.Protocols.HasFlag(MqttProtocol.Level5))
        {
            hub5 = new(logger, authenticationHandler, options.MQTT5)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        globalCts = new();
        connections = new();
        retained3 = new();
        retained5 = new();
        connStateObservers = new();
        updateStatsSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connStateMessageQueue = Channel.CreateBounded<ConnectionStateChangedMessage>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        if (meterFactory is not null)
        {
            var name = GetType().Namespace!;
            logger.LogMeterRegistered(name);
            RegisterMeters(meterFactory, name);
        }
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var localCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, stoppingToken);
        var token = linkedCts.Token;

        var notifierTask = RunConnectionStateNotifierAsync(token);
        var statsAggregateTask = RunStatsAggregatorAsync(token);

        try
        {
            var acceptors = new List<Task>();
            foreach (var (name, factory) in listenerFactories)
            {
                var listener = factory();
                logger.LogListenerRegistered(name, listener);
                acceptors.Add(AcceptConnectionsAsync(listener, token));
            }

            await Task.WhenAll(acceptors).ConfigureAwait(SuppressThrowing);
        }
        catch (OperationCanceledException)
        {
            /* expected */
        }
        finally
        {
            await localCts.CancelAsync().ConfigureAwait(SuppressThrowing);

            static async ValueTask WaitCompletedAsync(ConnectionSessionContext ctx) =>
                await ctx.RunAsync().ConfigureAwait(SuppressThrowing);
            await Parallel.ForEachAsync(connections, CancellationToken.None, (pair, _) => WaitCompletedAsync(pair.Value))
                .ConfigureAwait(SuppressThrowing);

            connStateMessageQueue.Writer.TryComplete();

            await Task.WhenAll(notifierTask, statsAggregateTask).ConfigureAwait(SuppressThrowing);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref state, Disposed) == Disposed)
        {
            return;
        }

        using (globalCts)
        {
            await globalCts.CancelAsync().ConfigureAwait(SuppressThrowing);
        }

        try
        {
            using (connStateObservers)
            {
                connStateObservers.NotifyCompleted();
            }
        }
        finally
        {
            await using (hub3)
            await using (hub4)
            await using (hub5) { }
        }
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    #region Implementation of IMqttServer

    public T? GetFeature<T>() where T : class
    {
        var type = typeof(T);
        return (type == typeof(IDataStatisticsFeature)
            || type == typeof(IConnectionStatisticsFeature)
            || type == typeof(ISubscriptionStatisticsFeature)
            || type == typeof(ISessionStatisticsFeature))
            && !RuntimeOptions.MetricsCollectionSupported ? null : this as T;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        switch (Interlocked.CompareExchange(ref state, Running, Stopped))
        {
            case Stopped:
                try
                {
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, globalCts.Token);
                    await ExecuteAsync(linkedCts.Token).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.CompareExchange(ref state, Stopped, Running);
                }

                break;
            case Running: ThrowHelper.ThrowInvalidState("Running"); break;
            case Disposed: ObjectDisposedException.ThrowIf(true, typeof(MqttServer)); break;
        }
    }

    #endregion
}