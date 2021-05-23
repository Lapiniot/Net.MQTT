using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Connections;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer : WorkerBase, IMqttServer
    {
        private ILogger<MqttServer> logger;
        private readonly ConcurrentDictionary<string, ConnectionSessionContext> connections;
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>> listeners;
        private readonly Dictionary<int, MqttProtocolHub> protocolHubs;

        public MqttServer(ILogger<MqttServer> logger, MqttProtocolHub[] protocolHubs)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.protocolHubs = (protocolHubs ?? throw new ArgumentNullException(nameof(protocolHubs)))
                .ToDictionary(f => f.ProtocolVersion, f => f);
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

            logger.LogListenerRegistered(name, listener);
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
    }
}