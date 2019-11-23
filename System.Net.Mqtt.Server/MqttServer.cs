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
    public sealed partial class MqttServer : IMqttServer, IAsyncDisposable
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

        public ILogger Logger { get; }

        public async ValueTask DisposeAsync()
        {
            if(disposed) return;
            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            finally
            {
                disposed = true;
            }
        }

        public bool RegisterListener(string name, IAsyncEnumerable<INetworkConnection> listener)
        {
            if(Volatile.Read(ref sentinel) == 0) return listeners.TryAdd(name, listener);

            throw new InvalidOperationException($"Invalid call to the {nameof(RegisterListener)} in the current state (already running).");
        }

        public Task RunAsync(CancellationToken stoppingToken)
        {
            if(disposed) throw new ObjectDisposedException(nameof(MqttServer));

            return processorTask = Interlocked.CompareExchange(ref sentinel, 1, 0) == 0
                ? StartProcessingAsync(stoppingToken)
                : throw new InvalidOperationException("Cannot start in the current state (already running).");
        }

        public async Task StopAsync()
        {
            var localProcessor = processorTask;
            var localTokenSource = globalCancellationSource;

            switch(Interlocked.CompareExchange(ref sentinel, 2, 1))
            {
                case 1:
                    using(localTokenSource)
                    {
                        localTokenSource.Cancel();
                        await localProcessor.ConfigureAwait(false);
                    }
                    break;
                case 2:
                    await localProcessor.ConfigureAwait(false);
                    break;
            }
        }

        private async Task StartProcessingAsync(CancellationToken stoppingToken)
        {
            var source = new CancellationTokenSource();
            try
            {
                using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token, stoppingToken);
                globalCancellationSource = source;
                var cancellationToken = linkedSource.Token;

                foreach(var (_, listener) in listeners)
                {
                    var unused = StartAcceptingClientsAsync(listener, cancellationToken);
                    LogInfo($"Start accepting incoming connections for {listener}");
                }

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
                if(Interlocked.Exchange(ref sentinel, 0) == 1)
                {
                    source.Dispose();
                }
            }
        }
    }
}