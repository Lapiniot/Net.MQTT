using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    public sealed class MqttBroker : IDisposable
    {
        private readonly ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)> listeners;
        private readonly object syncRoot;
        private bool disposed;
        private bool isListening;

        private readonly ConcurrentDictionary<MqttConnectionSession, bool> pendingSessions = new ConcurrentDictionary<MqttConnectionSession, bool>();
        private readonly ConcurrentDictionary<string, MqttConnectionSession> activeSessions = new ConcurrentDictionary<string, MqttConnectionSession>();

        public MqttBroker()
        {
            syncRoot = new object();
            listeners = new ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)>();
        }

        public bool IsListening => isListening;

        public void Dispose()
        {
            if(!disposed)
            {
                foreach(var listener in listeners)
                {
                    listener.Value.listener.Dispose();
                }

                disposed = true;
            }
        }

        public void Start()
        {
            if(!isListening)
            {
                lock(syncRoot)
                {
                    if(!isListening)
                    {
                        foreach(var pair in listeners)
                        {
                            Task.Run(() =>
                            {
                                var tuple = pair.Value;
                                tuple.tokenSource = new CancellationTokenSource();
                                Task.Run(() => StartAcceptingConnectionsAsync(tuple.listener, tuple.tokenSource.Token));
                            });
                        }

                        isListening = true;
                    }
                }
            }
        }


        public bool AddListener(string name, IConnectionListener listener)
        {
            return listeners.TryAdd(name, (listener, null));
        }

        private async Task StartAcceptingConnectionsAsync(IConnectionListener listener, CancellationToken cancellationToken)
        {
            listener.Start();

            while(!cancellationToken.IsCancellationRequested)
            {
                var transport = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);

                var session = new MqttConnectionSession(transport, this);

                AddPendingSession(session);

                cancellationToken.ThrowIfCancellationRequested();

                await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        internal void AddPendingSession(MqttConnectionSession session)
        {
            pendingSessions.TryAdd(session, false);
        }

        internal void AcceptSession(MqttConnectionSession session)
        {
            if(pendingSessions.TryRemove(session, out _))
            {
                activeSessions.TryAdd(session.ClientId, session);
            }
        }
    }
}