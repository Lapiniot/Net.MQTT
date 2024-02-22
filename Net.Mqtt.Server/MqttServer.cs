using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Net.Mqtt.Server.Protocol.V3;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Server;

public sealed partial class MqttServer : Worker, IMqttServer, IDisposable
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly ILogger<MqttServer> logger;
    private readonly ServerOptions options;
    private readonly IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories;
    private volatile TaskCompletionSource updateStatsSignal;
    private int disposed;
    private readonly ProtocolHub3? hub3;
    private readonly ProtocolHub4? hub4;
    private readonly ProtocolHub5? hub5;

    public MqttServer(ILogger<MqttServer> logger, ServerOptions options,
        IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories,
        IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger;
        this.options = options;
        this.listenerFactories = listenerFactories;

        if (options.Protocols.HasFlag(MqttProtocol.Level3))
        {
            hub3 = new(logger, options.AuthenticationHandler, options)
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
            hub4 = new(logger, options.AuthenticationHandler, options)
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
            hub5 = new(logger, options.AuthenticationHandler, options.MQTT5)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

            await Task.WhenAll(acceptors).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
        catch (OperationCanceledException)
        {
            /* expected */
        }
        finally
        {
            await localCts.CancelAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            static async ValueTask WaitCompletedAsync(ConnectionSessionContext ctx) =>
                await ctx.RunSessionAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            await Parallel.ForEachAsync(connections, CancellationToken.None, (pair, _) => WaitCompletedAsync(pair.Value))
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            connStateMessageQueue.Writer.TryComplete();

            await Task.WhenAll(notifierTask, statsAggregateTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        try
        {
            using (connStateObservers)
            {
                await base.DisposeAsync().ConfigureAwait(false);
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

    public T? GetFeature<T>() where T : class
    {
        var type = typeof(T);
        return (type == typeof(IDataStatisticsFeature)
            || type == typeof(IConnectionStatisticsFeature)
            || type == typeof(ISubscriptionStatisticsFeature)
            || type == typeof(ISessionStatisticsFeature))
            && !RuntimeSettings.MetricsCollectionSupport ? null : this as T;
    }
}