using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server
{
    public delegate MqttServerSession ServerSessionFactory(INetworkTransport transport, NetworkPipeReader reader);

    public sealed partial class MqttServer :
        IDisposable, IMqttServer,
        ISessionStateProvider<Protocol.V3.SessionState>,
        ISessionStateProvider<Protocol.V4.SessionState>
    {
        private readonly ConcurrentDictionary<string, MqttServerSession> activeSessions;
        private readonly TimeSpan connectTimeout;
        private readonly WorkerLoop<object> dispatcher;
        private readonly ConcurrentDictionary<string, (IConnectionListener Listener, WorkerLoop<IConnectionListener> Worker)> listeners;
        private readonly ParallelOptions parallelOptions;
        private readonly ServerSessionFactory[] protocols;
        private readonly object syncRoot;
        private bool disposed;

        public MqttServer()
        {
            syncRoot = new object();
            parallelMatchThreshold = 16;

            protocols = new ServerSessionFactory[]
            {
                (transport, reader) => new ServerSession(transport, reader, this, this),
                (transport, reader) => new Protocol.V4.ServerSession(transport, reader, this, this)
            };

            listeners = new ConcurrentDictionary<string, (IConnectionListener listener, WorkerLoop<IConnectionListener> Worker)>();
            activeSessions = new ConcurrentDictionary<string, MqttServerSession>();
            retainedMessages = new ConcurrentDictionary<string, Message>();
            connectTimeout = TimeSpan.FromSeconds(10);
            statesV3 = new ConcurrentDictionary<string, Protocol.V3.SessionState>();
            parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = 4};

            var channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
            dispatchQueueWriter = channel.Writer;
            dispatchQueueReader = channel.Reader;

            dispatcher = new WorkerLoop<object>(DispatchMessageAsync, null);
        }

        public bool IsListening { get; private set; }

        public void Dispose()
        {
            if(!disposed)
            {
                dispatcher.Dispose();

                foreach(var (listener, worker) in listeners.Values)
                {
                    try
                    {
                        worker.Dispose();
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

        public void Start()
        {
            if(!IsListening)
            {
                lock(syncRoot)
                {
                    if(!IsListening)
                    {
                        foreach(var (listener, worker) in listeners.Values)
                        {
                            listener.Start();
                            worker.Start();
                        }

                        dispatcher.Start();
                        IsListening = true;
                    }
                }
            }
        }

        public bool RegisterListener(string name, IConnectionListener listener)
        {
            lock(syncRoot)
            {
                var workerLoop = new WorkerLoop<IConnectionListener>(AcceptConnectionAsync, listener);

                if(listeners.TryAdd(name, (listener, workerLoop))) return true;

                workerLoop.Dispose();

                throw new ArgumentException(ListenerAlreadyRegistered);
            }
        }

        private static void TraceError(Exception exception)
        {
            Trace.TraceError(exception?.Message);
        }
    }
}