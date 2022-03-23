using System.Collections.Concurrent;
using System.Net.Connections;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

public class MqttServerOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed partial class MqttServer : Worker, IMqttServer
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly Dictionary<int, MqttProtocolHub> hubs;
    private readonly ConcurrentDictionary<string, IAsyncEnumerable<NetworkConnection>> listeners;
    private readonly MqttServerOptions options;
    private int disposed;
    private ParallelOptions parallelOptions;

    public MqttServer(ILogger<MqttServer> logger, IEnumerable<MqttProtocolHub> protocolHubs, MqttServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(protocolHubs);

        this.logger = logger;
        this.options = options;
        hubs = protocolHubs.ToDictionary(f => f.ProtocolLevel, f => f);
        listeners = new();
        connections = new();
        retainedMessages = new();
    }

    public void RegisterListener(string name, IAsyncEnumerable<NetworkConnection> listener)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException($"Invalid call to the {nameof(RegisterListener)} in the current state (already running)");
        }

        if (!listeners.TryAdd(name, listener))
        {
            throw new InvalidOperationException($"Listener with the same name '{name}' has been already registered");
        }

        LogListenerRegistered(name, listener);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                TaskScheduler = TaskScheduler.Default,
                CancellationToken = stoppingToken
            };

            await Task.WhenAll(listeners.Select(
                pair => StartAcceptingClientsAsync(pair.Value, stoppingToken))
            ).ConfigureAwait(false);
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
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0) return;

        static ValueTask DisposeCoreAsync<T>(T value) where T : class
        {
            if (value is IAsyncDisposable asyncDisposable) return asyncDisposable.DisposeAsync();
            if (value is IDisposable disposable) disposable.Dispose();
            return ValueTask.CompletedTask;
        }

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await Parallel.ForEachAsync(listeners, (pair, _) => DisposeCoreAsync(pair.Value)).ConfigureAwait(false);
            }
            finally
            {
                await Parallel.ForEachAsync(hubs, (pair, _) => DisposeCoreAsync(pair.Value)).ConfigureAwait(false);
            }
        }
    }
}