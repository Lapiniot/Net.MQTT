using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mqtt.Server.Implementations;
using System.Net.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.Reflection.BindingFlags;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Server
{
    public sealed class MqttServer : IDisposable,
        ISessionStateProvider<SessionStateV3>,
        ISessionStateProvider<SessionStateV4>
    {
        private const BindingFlags BindingFlags = Instance | NonPublic | Public;
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)> listeners;
        private readonly ConcurrentDictionary<MqttSession, bool> pendingSessions = new ConcurrentDictionary<MqttSession, bool>();
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
            listeners = new ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)>();
            connectTimeout = TimeSpan.FromSeconds(10);
            statesV3 = new ConcurrentDictionary<string, SessionStateV3>();
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

                    var _ = JoinClientAsync(connection).ContinueWith((task, state) =>
                    {
                        ((INetworkTransport)state).Dispose();
                        TraceError(task.Exception?.GetBaseException());
                    }, connection, NotOnRanToCompletion);
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

                var reader = new NetworkPipeReader(connection);

                MqttServerProtocol session = null;

                try
                {
                    await reader.ConnectAsync(token).ConfigureAwait(false);

                    var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, token).ConfigureAwait(false);

                    if((flags & TypeMask) != (byte)Connect)
                    {
                        throw new InvalidDataException(ConnectPacketExpected);
                    }

                    if(!MqttHelpers.TryReadString(buffer.Slice(offset), out var protocol, out var consumed) ||
                       string.IsNullOrEmpty(protocol))
                    {
                        throw new InvalidDataException(ProtocolNameExpected);
                    }

                    if(!MqttHelpers.TryReadByte(buffer.Slice(offset + consumed), out var level))
                    {
                        throw new InvalidDataException(ProtocolVersionExpected);
                    }

                    var impl = protocols.FirstOrDefault(i => i.Version == level);

                    if(impl.Type == null)
                    {
                        throw new InvalidDataException(NotSupportedProtocol);
                    }

                    RewindReader(reader, buffer);

                    session = (MqttServerProtocol)Activator.CreateInstance(impl.Type, BindingFlags, null,
                        new[] {connection, reader, impl.StateProvider}, null);

                    await session.ConnectAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    session?.Dispose();
                    reader.Dispose();
                    throw;
                }
            }

            void RewindReader(NetworkPipeReader reader, ReadOnlySequence<byte> buffer)
            {
                // Notify that we have not consumed any data from the pipe and 
                // cancel current pending Read operation to unblock any further 
                // immediate reads. Otherwise next reader will be blocked until 
                // new portion of data is read from network socket and flushed out
                // by writer task. Essentially, this is just a simulation of "Peek"
                // operation in terms of pipelines API.
                reader.AdvanceTo(buffer.Start, buffer.End);
                reader.CancelPendingRead();
            }
        }

        internal void AddPendingSession(MqttSession session)
        {
            pendingSessions.TryAdd(session, false);
        }

        private static void TraceError(Exception exception)
        {
            Trace.TraceError(exception?.Message);
        }

        #region ISessionStateProvider<ProtocolStateV3>

        private readonly ConcurrentDictionary<string, SessionStateV3> statesV3;

        SessionStateV3 ISessionStateProvider<SessionStateV3>.Create(string clientId)
        {
            var state = new SessionStateV3();
            return statesV3.AddOrUpdate(clientId, state, (ci, _) => state);
        }

        SessionStateV3 ISessionStateProvider<SessionStateV3>.Get(string clientId)
        {
            return statesV3.TryGetValue(clientId, out var state) ? state : default;
        }

        SessionStateV3 ISessionStateProvider<SessionStateV3>.Remove(string clientId)
        {
            statesV3.TryRemove(clientId, out var state);
            return state;
        }

        #endregion

        #region ISessionStateProvider<ProtocolStateV4>

        SessionStateV4 ISessionStateProvider<SessionStateV4>.Create(string clientId)
        {
            throw new NotImplementedException();
        }

        SessionStateV4 ISessionStateProvider<SessionStateV4>.Get(string clientId)
        {
            throw new NotImplementedException();
        }

        SessionStateV4 ISessionStateProvider<SessionStateV4>.Remove(string clientId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}