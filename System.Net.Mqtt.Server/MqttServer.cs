using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Mqtt.Server.Implementations;
using System.Threading;
using System.Threading.Channels;
using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer : IDisposable, IObserver<Message>,
        ISessionStateProvider<SessionStateV3>, ISessionStateProvider<SessionStateV4>
    {
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, (IConnectionListener Listener, WorkerLoop<IConnectionListener> Worker)> listeners;
        private readonly (byte Version, Type Type, object StateProvider)[] protocols;
        private readonly object syncRoot;
        private bool disposed;

        public MqttServer()
        {
            syncRoot = new object();
            protocols = new (byte Version, Type Type, object StateProvider)[]
            {
                (0x03, typeof(MqttServerSessionV3), this),
                (0x04, typeof(MqttServerSessionV4), this)
            };
            listeners = new ConcurrentDictionary<string,
                (IConnectionListener listener, WorkerLoop<IConnectionListener> Worker)>();
            connectTimeout = TimeSpan.FromSeconds(10);
            statesV3 = new ConcurrentDictionary<string, SessionStateV3>();
            distributionChannel = Channel.CreateUnbounded<Message>();
        }

        public bool IsListening { get; private set; }

        public void Dispose()
        {
            if(!disposed)
            {
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
                        foreach(var (_, (listener, worker)) in listeners)
                        {
                            listener.Start();
                            worker.Start();
                        }

                        IsListening = true;
                    }
                }
            }
        }


        //internal void Dispatch(PublishPacket packet)
        //{
        //    foreach(var session in activeSessions.Values)
        //    {
        //        if(session.IsInterested(packet.Topic, out var level))
        //        {
        //            var adjustedQoS = (QoSLevel)Min((byte)packet.QoSLevel, (byte)level);

        //            session.Dispatch(packet.Topic, packet.Payload, adjustedQoS);
        //        }
        //    }
        //}

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