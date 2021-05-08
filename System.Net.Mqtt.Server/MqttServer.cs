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
    public sealed partial class MqttServer : WorkerBase
    {
        private ILogger<MqttServer> logger;
        private readonly ConcurrentDictionary<string, ConnectionContext> connections;
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>> listeners;
        private readonly Dictionary<int, MqttProtocolHub> protocolHubs;

        public MqttServer(ILogger<MqttServer> logger, params MqttProtocolHub[] protocolHubs)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.protocolHubs = protocolHubs.ToDictionary(f => f.ProtocolVersion, f => f);
            listeners = new ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>>();
            connections = new ConcurrentDictionary<string, ConnectionContext>();
            retainedMessages = new ConcurrentDictionary<string, Message>();
            connectTimeout = TimeSpan.FromSeconds(10);

            (dispatchQueueReader, dispatchQueueWriter) = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });
        }

        public bool RegisterListener(string name, IAsyncEnumerable<INetworkConnection> listener)
        {
            if(!IsRunning) return listeners.TryAdd(name, listener);

            throw new InvalidOperationException($"Invalid call to the {nameof(RegisterListener)} in the current state (already running).");
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

                    Parallel.ForEach(protocolHubs.Values, protocol => protocol.DispatchMessage(message));
                }
            }
            catch(OperationCanceledException) { }
            finally
            {
                if(acceptors != null)
                {
                    await Task.WhenAll(acceptors).ConfigureAwait(false);
                }

                await Task.WhenAll(connections.Values.Select(v => v.CompletionLazy.Value).ToArray()).ConfigureAwait(false);
            }
        }
    }
}