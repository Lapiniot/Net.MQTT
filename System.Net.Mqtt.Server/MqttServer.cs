using System.Collections.Concurrent;
using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : Worker, IMqttServer, IDisposable
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly ILogger<MqttServer> logger;
    private readonly MqttServerOptions options;
    private readonly IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories;
    private volatile TaskCompletionSource updateStatsSignal;
    private int disposed;
    private readonly ProtocolHub3? hub3;
    private readonly ProtocolHub4? hub4;
    private readonly ProtocolHub5? hub5;

    public MqttServer(ILogger<MqttServer> logger, MqttServerOptions options,
        IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger;
        this.options = options;
        this.listenerFactories = listenerFactories;

        if (options.Protocols.HasFlag(MqttProtocols.Level3))
        {
            hub3 = new ProtocolHub3(logger, options.AuthenticationHandler, options.MaxInFlight, options.MaxUnflushedBytes)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        if (options.Protocols.HasFlag(MqttProtocols.Level4))
        {
            hub4 = new ProtocolHub4(logger, options.AuthenticationHandler, options.MaxInFlight, options.MaxUnflushedBytes)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        if (options.Protocols.HasFlag(MqttProtocols.Level5))
        {
            hub5 = new ProtocolHub5(logger, options.AuthenticationHandler, options.MaxInFlight, options.MaxUnflushedBytes)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        connections = new();
        retainedMessages = new();
        connStateObservers = new();
        updateStatsSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connStateMessageQueue = Channel.CreateBounded<ConnectionStateChangedMessage>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var notifierTask = RunConnectionStateNotifierAsync(stoppingToken);
        var statsAggregateTask = RunStatsAggregatorAsync(stoppingToken);

        try
        {
            await Task.WhenAll(listenerFactories.Select(pair =>
            {
                var (name, factory) = pair;
                var listener = factory();
                logger.LogListenerRegistered(name, listener);
                return AcceptConnectionsAsync(listener, stoppingToken);
            })).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* expected */
        }
        finally
        {
            try
            {
                static async ValueTask WaitCompletedAsync(ConnectionSessionContext ctx) => await ctx.WaitCompletedAsync().ConfigureAwait(false);

                await Parallel.ForEachAsync(connections, CancellationToken.None, (pair, _) => WaitCompletedAsync(pair.Value)).ConfigureAwait(false);
            }
            finally
            {
                connStateMessageQueue.Writer.TryComplete();
                await Task.WhenAll(notifierTask, statsAggregateTask).ConfigureAwait(false);
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
        {
            return;
        }

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