using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Math;

namespace System.Net.Mqtt.Server
{
    public sealed class MqttBroker : IDisposable
    {
        private readonly ConcurrentDictionary<string, MqttSession> activeSessions = new ConcurrentDictionary<string, MqttSession>();
        private readonly ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)> listeners;
        private readonly ConcurrentDictionary<MqttSession, bool> pendingSessions = new ConcurrentDictionary<MqttSession, bool>();
        private readonly object syncRoot;
        private bool disposed;
        private bool isListening;

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
                foreach(var (listener, _) in listeners.Values)
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

                foreach(var session in activeSessions.Values)
                {
                    try
                    {
                        session.Dispose();
                    }
                    catch
                    {
                        //ignored
                    }
                }

                foreach(var session in pendingSessions.Keys)
                {
                    try
                    {
                        session.Dispose();
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

        internal void Dispatch(PublishPacket packet)
        {
            foreach(var session in activeSessions.Values)
            {
                if(session.IsInterested(packet.Topic, out var level))
                {
                    var adjustedQoS = (QoSLevel)Min((byte)packet.QoSLevel, (byte)level);

                    session.Dispatch(packet.Topic, packet.Payload, adjustedQoS);
                }
            }
        }

        public bool AddListener(string name, IConnectionListener listener)
        {
            lock(syncRoot)
            {
                return listeners.TryAdd(name, (listener, null));
            }
        }

        private async Task StartAcceptingConnectionsAsync(IConnectionListener listener, CancellationToken cancellationToken)
        {
            listener.Start();

            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var transport = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);

                    var session = new MqttSession(transport, this);

                    AddPendingSession(session);

                    cancellationToken.ThrowIfCancellationRequested();

                    await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
                }
                catch(Exception exception) when(!(exception is OperationCanceledException))
                {
                    Trace.TraceError(exception.Message);
                }
            }
        }

        internal void AddPendingSession(MqttSession session)
        {
            pendingSessions.TryAdd(session, false);
        }

        internal void Join(MqttSession session)
        {
            if(pendingSessions.TryRemove(session, out _))
            {
                activeSessions.TryAdd(session.ClientId, session);
            }
        }
    }
}