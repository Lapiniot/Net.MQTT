using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Implementations;
using System.Threading;
using System.Threading.Tasks;
using static System.Math;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Server
{
    public sealed class MqttServer : IDisposable
    {
        private readonly ConcurrentDictionary<string, MqttSession> activeSessions = new ConcurrentDictionary<string, MqttSession>();
        private readonly TimeSpan connectTimeout;
        private readonly MqttProtocolFactory protocolFactory;
        private readonly ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)> listeners;
        private readonly ConcurrentDictionary<MqttSession, bool> pendingSessions = new ConcurrentDictionary<MqttSession, bool>();
        private readonly object syncRoot;
        private bool disposed;

        public MqttServer()
        {
            syncRoot = new object();
            listeners = new ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)>();
            protocolFactory = new MqttProtocolFactory(
                (0x03, typeof(MqttProtocolV3_1_0)),
                (0x04, typeof(MqttProtocolV3_1_1)));
            connectTimeout = TimeSpan.FromSeconds(10);
        }

        public bool IsListening { get; private set; }

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
            if(!IsListening)
            {
                lock(syncRoot)
                {
                    if(!IsListening)
                    {
                        foreach(var (_, value) in listeners)
                        {
                            var tuple = value;
                            tuple.tokenSource = new CancellationTokenSource();
                            Task.Run(() => StartAcceptingConnectionsAsync(tuple.listener, tuple.tokenSource.Token));
                        }

                        IsListening = true;
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
                    var connection = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    var _ = JoinClientAsync(connection).ContinueWith(TraceError, null, NotOnRanToCompletion);
                }
                catch(Exception exception) when(!(exception is OperationCanceledException))
                {
                    TraceError(exception);
                }
            }
        }

        private async Task JoinClientAsync(INetworkTransport connection)
        {
            using(var cts = new CancellationTokenSource(connectTimeout))
            {
                var token = cts.Token;

                var protocol = await protocolFactory.DetectProtocolAsync(connection, token).ConfigureAwait(false);

                await protocol.ConnectAsync(token).ConfigureAwait(false);
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

        private static void TraceError(Task task, object arg2)
        {
            if(task?.Exception != null)
            {
                TraceError(task.Exception.GetBaseException());
            }
        }

        private static void TraceError(Exception exception)
        {
            Trace.TraceError(exception.Message);
        }
    }
}