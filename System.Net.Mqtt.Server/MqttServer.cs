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
        private readonly ConcurrentDictionary<string, (INetworkConnection Connection, MqttServerSession Session, Lazy<Task> Task)> connections;
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>> listeners;
        private readonly Dictionary<int, MqttProtocolHub> protocolHubs;
        private bool disposed;
        private CancellationTokenSource globalCancellationSource;
        private Task processorTask;
        private int sentinel;

        public MqttServer(ILogger logger, params MqttProtocolHub[] protocolHubs)
        {
            Logger = logger;
            this.protocolHubs = protocolHubs.ToDictionary(f => f.ProtocolVersion, f => f);
            listeners = new ConcurrentDictionary<string, IAsyncEnumerable<INetworkConnection>>();
            connections = new ConcurrentDictionary<string, (INetworkConnection, MqttServerSession, Lazy<Task>)>();
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
            if(Volatile.Read(ref sentinel) == 0) return listeners.TryAdd(name, listener);

            throw new InvalidOperationException($"Invalid call to the {nameof(RegisterListener)} in the current state (already running).");
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Task[] acceptors = null;
            try
            {
                acceptors = listeners.Select(p => StartAcceptingClientsAsync(p.Value, cancellationToken)).ToArray();

                while(!cancellationToken.IsCancellationRequested)
                {
                    var vt = dispatchQueueReader.ReadAsync(cancellationToken);

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

                await Task.WhenAll(connections.Values.Select(v => v.Task.Value).ToArray()).ConfigureAwait(false);
            }
        }
    }
}