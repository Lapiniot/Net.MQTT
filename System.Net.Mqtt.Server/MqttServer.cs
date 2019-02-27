using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Listeners;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer : IMqttServer, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<string, MqttServerSession> activeSessions;
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, AsyncConnectionListener> listeners;
        private readonly Dictionary<int, MqttProtocolHub> protocolHubs;
        private bool disposed;
        private CancellationTokenSource globalCancellationSource;
        private Task processorTask;

        public MqttServer(ILogger logger, params MqttProtocolHub[] protocolHubs)
        {
            Logger = logger;
            this.protocolHubs = protocolHubs.ToDictionary(f => f.ProtocolVersion, f => f);
            listeners = new ConcurrentDictionary<string, AsyncConnectionListener>();
            activeSessions = new ConcurrentDictionary<string, MqttServerSession>();
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
            if(!disposed)
            {
                foreach(var listener in listeners.Values)
                {
                    using(listener) {}
                }

                await TerminateAsync().ConfigureAwait(false);

                disposed = true;
            }
        }

        public bool RegisterListener(string name, AsyncConnectionListener listener)
        {
            if(Volatile.Read(ref globalCancellationSource) == null)
            {
                return listeners.TryAdd(name, listener);
            }

            throw new InvalidOperationException("Invalid call to the " + nameof(RegisterListener) + " in this state (already running).");
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            using(var tokenSource = new CancellationTokenSource())
            {
                if(Interlocked.CompareExchange(ref globalCancellationSource, tokenSource, null) == null)
                {
                    using(var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, stoppingToken))
                    {
                        processorTask = StartProcessingAsync(linkedSource.Token);
                        try
                        {
                            await processorTask.ConfigureAwait(false);
                        }
                        catch(OperationCanceledException) {}
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid call to the {nameof(RunAsync)} (already running).");
                }
            }
        }

        public async Task TerminateAsync()
        {
            if(Volatile.Read(ref globalCancellationSource) != null)
            {
                try
                {
                    globalCancellationSource.Cancel();
                    await processorTask.ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    Interlocked.Exchange(ref globalCancellationSource, null);
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid call to the " + nameof(TerminateAsync) + " in this state (is not running).");
            }
        }

        private async Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            foreach(var (_, listener) in listeners)
            {
                var unused = StartAcceptingClientsAsync(listener, cancellationToken);
                LogInfo($"Start accepting incoming connections for {listener}");
            }

            while(!cancellationToken.IsCancellationRequested)
            {
                var vt = dispatchQueueReader.ReadAsync(cancellationToken);

                var message = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

                Parallel.ForEach(protocolHubs.Values, protocol => protocol.DispatchMessage(message));
            }
        }
    }
}