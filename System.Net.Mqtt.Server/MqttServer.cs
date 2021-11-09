using System.Collections.Concurrent;
using System.Net.Connections;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : WorkerBase, IMqttServer, IDisposable
{
    private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
    private readonly TimeSpan connectTimeout;
    private readonly ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>> listeners;
    private readonly Dictionary<int, MqttProtocolHub> protocolHubs;
    private bool disposed;

    public MqttServer(ILogger<MqttServer> logger, MqttProtocolHub[] protocolHubs)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(protocolHubs);

        this.logger = logger;
        this.protocolHubs = protocolHubs.ToDictionary(f => f.ProtocolLevel, f => f);
        listeners = new ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>>();
        connections = new ConcurrentDictionary<string, ConnectionSessionContext>();
        retainedMessages = new ConcurrentDictionary<string, Message>();
        connectTimeout = TimeSpan.FromSeconds(10);

        (dispatchQueueReader, dispatchQueueWriter) = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
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

            while(!stoppingToken.IsCancellationRequested)
            {
                var vt = dispatchQueueReader.ReadAsync(stoppingToken);

                var message = vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);

                ValueTask DispatchAsync(MqttProtocolHub protocolHub, CancellationToken cancellationToken)
                {
                    return protocolHub.DispatchMessageAsync(message, cancellationToken);
                }

                await Parallel.ForEachAsync(protocolHubs.Values, stoppingToken, DispatchAsync).ConfigureAwait(false);
            }
        }
        catch(OperationCanceledException) { }
        finally
        {
            if(acceptors != null)
            {
                await Task.WhenAll(acceptors).ConfigureAwait(false);
            }

            await Task.WhenAll(connections.Values.Select(v => v.Completion)).ConfigureAwait(false);
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

            foreach(var hub in protocolHubs.Values)
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