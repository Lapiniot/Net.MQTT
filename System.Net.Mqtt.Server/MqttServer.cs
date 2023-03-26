using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public class MqttServerOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public sealed partial class MqttServer : Worker, IMqttServer, IDisposable
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly Dictionary<int, MqttProtocolHub> hubs;
    private readonly ILogger<MqttServer> logger;
    private readonly MqttServerOptions options;
    private readonly IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories;
    private readonly Func<MqttServerSession, CancellationToken, Task> defferedStartup;
    private int disposed;

    public MqttServer(ILogger<MqttServer> logger, MqttServerOptions options,
        IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories,
        IEnumerable<MqttProtocolHub> protocolHubs)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(protocolHubs);

        this.logger = logger;
        this.options = options;
        this.listenerFactories = listenerFactories;
        hubs = protocolHubs.ToDictionary(f => f.ProtocolLevel, f => f);
        connections = new();
        retainedMessages = new();
        defferedStartup = RunSessionAsync;
        connStateObservers = new();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        connStateMessageQueue = Channel.CreateBounded<ConnectionStateChangedMessage>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
        var notifierTask = RunConnectionStateNotifierAsync(stoppingToken);

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
                static async ValueTask WaitCompletedAsync(ConnectionSessionContext ctx) => await ctx.RunAsync().ConfigureAwait(false);

                await Parallel.ForEachAsync(connections, CancellationToken.None, (pair, _) => WaitCompletedAsync(pair.Value)).ConfigureAwait(false);
            }
            finally
            {
                connStateMessageQueue.Writer.TryComplete();
                await notifierTask.ConfigureAwait(false);
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

    public T GetFeature<T>() where T : class
    {
        var featureType = typeof(T);
        return (featureType == typeof(IDataStatisticsFeature)
            || featureType == typeof(IConnectionStatisticsFeature))
            && !RuntimeSettings.MetricsCollectionSupport ? null : this as T;
    }
}