using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Mqtt.Server.Protocol.V3;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public delegate MqttServerSession ServerSessionFactory(INetworkTransport transport, PipeReader reader);

    public sealed partial class MqttServer : IDisposable, IMqttServer
    {
        private readonly ConcurrentDictionary<string, MqttServerSession> activeSessions;
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, IConnectionListener> listeners;
        private readonly ParallelOptions parallelOptions;
        private readonly ServerSessionFactory[] protocols;
        private readonly ConcurrentDictionary<string, Protocol.V3.SessionState> statesV3;
        private readonly ConcurrentDictionary<string, Protocol.V4.SessionState> statesV4;
        private bool disposed;
        private CancellationTokenSource globalCancellationSource;
        private Task processorTask;

        public MqttServer()
        {
            parallelMatchThreshold = 16;

            statesV3 = new ConcurrentDictionary<string, Protocol.V3.SessionState>();
            statesV4 = new ConcurrentDictionary<string, Protocol.V4.SessionState>();
            var spv3 = new SessionStateProvider(statesV3);
            var spv4 = new Protocol.V4.SessionStateProvider(statesV4);
            protocols = new ServerSessionFactory[]
            {
                (transport, reader) => new ServerSession(transport, reader, spv3, this),
                (transport, reader) => new Protocol.V4.ServerSession(transport, reader, spv4, this)
            };

            listeners = new ConcurrentDictionary<string, IConnectionListener>();
            activeSessions = new ConcurrentDictionary<string, MqttServerSession>();
            retainedMessages = new ConcurrentDictionary<string, Message>();
            connectTimeout = TimeSpan.FromSeconds(10);
            parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = 4};

            var channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

            dispatchQueueWriter = channel.Writer;
            dispatchQueueReader = channel.Reader;
        }

        public void Dispose()
        {
            if(!disposed)
            {
                foreach(var listener in listeners.Values)
                {
                    try
                    {
                        listener.Dispose();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                disposed = true;
            }
        }

        public bool RegisterListener(string name, IConnectionListener listener)
        {
            if(Volatile.Read(ref globalCancellationSource) == null)
            {
                return listeners.TryAdd(name, listener);
            }

            throw new InvalidOperationException("Invalid call to the " + nameof(RegisterListener) + " in this state (already running).");
        }

        private static void TraceError(Exception exception)
        {
            Trace.TraceError(exception?.Message);
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
                        catch(OperationCanceledException)
                        {
                        }
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
            }

            while(!cancellationToken.IsCancellationRequested)
            {
                await DispatchMessageAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}