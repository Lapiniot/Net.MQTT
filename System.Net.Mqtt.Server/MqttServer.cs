using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Implementations;
using System.Net.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.Math;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.Reflection.BindingFlags;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Server
{
    public sealed class MqttServer : IDisposable
    {
        private const BindingFlags BindingFlags = Instance | NonPublic | Public;
        private readonly ConcurrentDictionary<string, MqttSession> activeSessions = new ConcurrentDictionary<string, MqttSession>();
        private readonly TimeSpan connectTimeout;
        private readonly ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)> listeners;
        private readonly ConcurrentDictionary<MqttSession, bool> pendingSessions = new ConcurrentDictionary<MqttSession, bool>();
        private readonly (byte Version, Type Type)[] protocols;
        private readonly object syncRoot;
        private bool disposed;

        public MqttServer()
        {
            syncRoot = new object();
            protocols = new (byte Version, Type Type)[]
            {
                (0x03, typeof(MqttProtocolV3)),
                (0x04, typeof(MqttProtocolV4))
            };
            listeners = new ConcurrentDictionary<string, (IConnectionListener listener, CancellationTokenSource tokenSource)>();
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

                MqttProtocol session = null;

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

                    var type = protocols.FirstOrDefault(i => i.Version == level).Type;

                    if(type == null)
                    {
                        throw new InvalidDataException(NotSupportedProtocol);
                    }

                    RewindReader(reader, buffer);

                    session = (MqttProtocol)Activator.CreateInstance(type, BindingFlags, null,
                        new object[] {connection, reader}, null);

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

        internal void Join(MqttSession session)
        {
            if(pendingSessions.TryRemove(session, out _))
            {
                activeSessions.TryAdd(session.ClientId, session);
            }
        }

        private static void TraceError(Exception exception)
        {
            Trace.TraceError(exception?.Message);
        }
    }
}