using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public class MqttServerOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed partial class MqttServer : Worker, IMqttServer, IDisposable
{
    private readonly Action<object> cancelDelayedCallback;
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly Dictionary<int, MqttProtocolHub> hubs;
    private readonly MqttServerOptions options;
    private readonly IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> listenerFactories;
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
        cancelDelayedCallback = state => ((CancellationTokenSource)state).CancelAfter(this.options.DisconnectTimeout);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.WhenAll(listenerFactories.Select(pair =>
            {
                var (name, factory) = pair;
                var listener = factory();
                LogListenerRegistered(name, listener);
                return StartAcceptingClientsAsync(listener, stoppingToken);
            })).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* expected */
        }
        finally
        {
            static async ValueTask WaitCompletedAsync(ConnectionSessionContext ctx) => await ctx.Completion.ConfigureAwait(false);

            await Parallel.ForEachAsync(connections, CancellationToken.None, (pair, _) => WaitCompletedAsync(pair.Value)).ConfigureAwait(false);
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
            await base.DisposeAsync().ConfigureAwait(false);
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
}