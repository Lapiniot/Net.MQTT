using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Mqtt.Server.Implementations;
using System.Net.Mqtt.Server.Properties;
using System.Net.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketFlags;
using static System.Reflection.BindingFlags;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private const BindingFlags BindingFlags = Instance | NonPublic | Public;

        private async Task AcceptConnectionAsync(IConnectionListener listener, CancellationToken cancellationToken)
        {
            INetworkTransport connection = null;
            try
            {
                connection = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var _ = JoinClientAsync(connection, cancellationToken).ContinueWith((task, state) =>
                {
                    ((INetworkTransport)state).Dispose();
                    TraceError(task.Exception?.GetBaseException());
                }, connection, NotOnRanToCompletion);
            }
            catch(Exception exception)
            {
                connection?.Dispose();
                TraceError(exception);
            }
        }

        private async Task<MqttServerSession> JoinClientAsync(INetworkTransport connection, CancellationToken cancellationToken)
        {
            using(var timeoutSource = new CancellationTokenSource(connectTimeout))
            using(var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken))
            {
                var token = linkedSource.Token;

                var reader = new NetworkPipeReader(connection);

                MqttServerSession session = null;

                try
                {
                    await reader.ConnectAsync(token).ConfigureAwait(false);

                    var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, token).ConfigureAwait(false);

                    if((flags & TypeMask) != (byte)PacketType.Connect)
                    {
                        throw new InvalidDataException(Strings.ConnectPacketExpected);
                    }

                    if(!MqttHelpers.TryReadString(buffer.Slice(offset), out var protocol, out var consumed) ||
                       string.IsNullOrEmpty(protocol))
                    {
                        throw new InvalidDataException(Strings.ProtocolNameExpected);
                    }

                    if(!MqttHelpers.TryReadByte(buffer.Slice(offset + consumed), out var level))
                    {
                        throw new InvalidDataException(Strings.ProtocolVersionExpected);
                    }

                    var impl = protocols.FirstOrDefault(i => i.Version == level);

                    if(impl.Type == null)
                    {
                        throw new InvalidDataException(Strings.NotSupportedProtocol);
                    }

                    RewindReader(reader, buffer);

                    session = (MqttServerSession)Activator.CreateInstance(impl.Type, BindingFlags, null,
                        new[] { connection, reader, impl.StateProvider, this }, null);

                    await session.ConnectAsync(token).ConfigureAwait(false);

                    activeSessions.AddOrUpdate(session.ClientId, session, (cid, existing) =>
                    {
                        try
                        {
                            existing.CloseSessionAsync();
                        }
                        catch
                        {
                            
                        }
                        return session;
                    });

                    return session;
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
    }
}