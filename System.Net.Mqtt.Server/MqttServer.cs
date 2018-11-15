using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Mqtt.Server.Implementations;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server
{
    public delegate MqttServerSession ServerSessionFactory(INetworkTransport transport, NetworkPipeReader reader);
    public sealed partial class MqttServer : IDisposable, IObserver<Message>,
        ISessionStateProvider<SessionStateV3>, ISessionStateProvider<SessionStateV4>
    {
        private readonly ConcurrentDictionary<string, MqttServerSession> activeSessions;
        private readonly TimeSpan connectTimeout;
        private readonly WorkerLoop<object> dispatcher;
        private readonly ConcurrentDictionary<string, (IConnectionListener Listener, WorkerLoop<IConnectionListener> Worker)> listeners;
        private readonly ParallelOptions parallelOptions;
        private readonly (byte Version, ServerSessionFactory Factory)[] protocols;
        private readonly object syncRoot;
        private bool disposed;

        public MqttServer()
        {
            syncRoot = new object();
            protocols = new (byte Version, ServerSessionFactory Factory)[]
            {
                (0x03, (transport, reader) => new MqttServerSessionV3(transport, reader, this, this)),
                (0x04, (transport, reader) => new MqttServerSessionV4(transport, reader, this, this))
            };
            listeners = new ConcurrentDictionary<string, (IConnectionListener listener, WorkerLoop<IConnectionListener> Worker)>();
            activeSessions = new ConcurrentDictionary<string, MqttServerSession>();
            connectTimeout = TimeSpan.FromSeconds(10);
            statesV3 = new ConcurrentDictionary<string, SessionStateV3>();
            parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
            distributionChannel = Channel.CreateUnbounded<Message>();
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