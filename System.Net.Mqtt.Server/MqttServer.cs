using System.Collections.Concurrent;
using System.Net.Connections;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

public class MqttServerOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed partial class MqttServer : Worker, IMqttServer, IDisposable
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>> listeners;
    private readonly Dictionary<int, MqttProtocolHub> hubs;
    private readonly MqttServerOptions options;
    private bool disposed;
    private ParallelOptions parallelOptions;

    public MqttServer(ILogger<MqttServer> logger, MqttProtocolHub[] protocolHubs, MqttServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(protocolHubs);

        this.logger = logger;
        this.options = options;
        hubs = protocolHubs.ToDictionary(f => f.ProtocolLevel, f => f);
        listeners = new ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>>();
        connections = new ConcurrentDictionary<string, ConnectionSessionContext>();
        retainedMessages = new ConcurrentDictionary<string, Message>();
    }

    public void RegisterListener(string name, IAsyncEnumerable<INetworkConnection> listener)
    {
        if(IsRunning)
        {
            throw new InvalidOperationException($"Invalid call to the {nameof(RegisterListener)} in the current state (already running)");
        }

        if(!listeners.TryAdd(name, listener))
        {
            throw new InvalidOperationException($"Listener with the same name '{name}' has been already registered");
        }

        LogListenerRegistered(name, listener);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                TaskScheduler = TaskScheduler.Default,
                CancellationToken = stoppingToken
            };

            await Task.WhenAll(listeners.Values.Select(
                listener => StartAcceptingClientsAsync(listener, stoppingToken)
            )).ConfigureAwait(false);
        }
        catch(OperationCanceledException) { /* expected */ }
        finally
        {
            static async ValueTask WaitCompletedAsync(ConnectionSessionContext connection, CancellationToken _)
            {
                var task = connection.Completion;
                if(!task.IsCompletedSuccessfully)
                {
                    await task.ConfigureAwait(false);
                }
            }

            await Parallel.ForEachAsync(connections.Values, CancellationToken.None, WaitCompletedAsync).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if(disposed) return;

        foreach(var listener in listeners)
        {
            (listener.Value as IDisposable)?.Dispose();
        }

        foreach(var hub in hubs.Values)
        {
            if(hub as IAsyncDisposable is { } disposable)
                disposable.DisposeAsync();
        }

        disposed = true;
    }

    public override ValueTask DisposeAsync()
    {
        Dispose();
        return base.DisposeAsync();
    }
}