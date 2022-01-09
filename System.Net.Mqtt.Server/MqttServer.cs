using System.Collections.Concurrent;
using System.Net.Connections;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;
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

        (messageQueueReader, messageQueueWriter) = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });
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
        Task[] acceptors = null;
        try
        {
            acceptors = listeners.Select(p => StartAcceptingClientsAsync(p.Value, stoppingToken)).ToArray();

            parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                TaskScheduler = TaskScheduler.Default,
                CancellationToken = stoppingToken
            };

            while(!stoppingToken.IsCancellationRequested)
            {
                var vt = messageQueueReader.ReadAsync(stoppingToken);

                var message = vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);

                foreach(var (_, hub) in hubs)
                {
                    hub.DispatchMessage(message);
                }
            }
        }
        catch(OperationCanceledException) { /* expected */ }
        finally
        {
            if(acceptors != null)
            {
                await Task.WhenAll(acceptors).ConfigureAwait(false);
            }

            static async ValueTask WaitCompletedAsync(ConnectionSessionContext connection, CancellationToken _)
            {
                var task = connection.Completion;
                if(!task.IsCompletedSuccessfully)
                {
                    await task.ConfigureAwait(false);
                }
            }

            await Parallel.ForEachAsync(connections.Values, WaitCompletedAsync).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if(!disposed)
        {
            foreach(var listener in listeners)
            {
                (listener.Value as IDisposable)?.Dispose();
            }

            foreach(var hub in hubs.Values)
            {
                (hub as IDisposable)?.Dispose();
            }

            disposed = true;
        }
    }

    public override ValueTask DisposeAsync()
    {
        Dispose();
        return base.DisposeAsync();
    }
}