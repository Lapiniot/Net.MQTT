using System.Collections.Concurrent;
using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;

namespace System.Net.Mqtt.Server;

[Flags]
public enum ProtocolLevel
{
    Level3 = 0b001,
    Level4 = 0b010,
    Level5 = 0b100
}

public record class MqttServerOptions
{
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public ushort MaxInFlight { get; init; } = (ushort)short.MaxValue;
    public int MaxUnflushedBytes { get; set; } = 8096;
    public ProtocolLevel Level { get; init; } = ProtocolLevel.Level3 | ProtocolLevel.Level4 | ProtocolLevel.Level5;
    public IMqttAuthenticationHandler? AuthenticationHandler { get; init; }
}

public sealed partial class MqttServer : Worker, IMqttServer, IDisposable
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly Dictionary<int, MqttProtocolHub> hubs;
    private readonly ILogger<MqttServer> logger;
    private readonly MqttServerOptions options;
    private readonly IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories;
    private volatile TaskCompletionSource updateStatsSignal;
    private int disposed;

    public MqttServer(ILogger<MqttServer> logger, MqttServerOptions options,
        IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger;
        this.options = options;
        this.listenerFactories = listenerFactories;

        hubs = new Dictionary<int, MqttProtocolHub>(3);
        if (options.Level.HasFlag(ProtocolLevel.Level3))
        {
            hubs[3] = new ProtocolHub3(logger, options.AuthenticationHandler, options.MaxInFlight, options.MaxUnflushedBytes)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        if (options.Level.HasFlag(ProtocolLevel.Level4))
        {
            hubs[4] = new ProtocolHub4(logger, options.AuthenticationHandler, options.MaxInFlight, options.MaxUnflushedBytes)
            {
                IncomingObserver = this,
                SubscribeObserver = this,
                UnsubscribeObserver = this,
                PacketRxObserver = this,
                PacketTxObserver = this
            };
        }

        if (options.Level.HasFlag(ProtocolLevel.Level5))
        {
            hubs[5] = new ProtocolHub5(logger, options.AuthenticationHandler, options.MaxInFlight, options.MaxUnflushedBytes)
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
            await Parallel.ForEachAsync(hubs, static (pair, _) =>
            {
                switch (pair.Value)
                {
                    case IAsyncDisposable asyncDisposable:
                        return asyncDisposable.DisposeAsync();
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
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