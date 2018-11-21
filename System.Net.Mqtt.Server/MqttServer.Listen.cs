using System.IO;
using System.Linq;
using System.Net.Mqtt.Server.Properties;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketFlags;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
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

                try
                {
                    await reader.ConnectAsync(token).ConfigureAwait(false);

                    var factory = await DetectProtocolAsync(reader, token).ConfigureAwait(false);

                    return await StartSessionAsync(factory, connection, reader, token).ConfigureAwait(false);
                }
                catch
                {
                    reader.Dispose();
                    throw;
                }
            }
        }

        private async Task<MqttServerSession> StartSessionAsync(ServerSessionFactory factory,
            INetworkTransport connection, NetworkPipeReader reader, CancellationToken token)
        {
            var session = factory(connection, reader);
            try
            {
                await session.AcceptConnectionAsync(token).ConfigureAwait(false);

                activeSessions.AddOrUpdate(session.ClientId, session, (clientId, existing) =>
                 {
                     existing.CloseSessionAsync();
                    return session;
                 });

                await session.ConnectAsync(token).ConfigureAwait(false);

                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private async Task<ServerSessionFactory> DetectProtocolAsync(NetworkPipeReader reader, CancellationToken token)
        {
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

            if(impl.Factory == null)
            {
                throw new InvalidDataException(Strings.NotSupportedProtocol);
            }

            RewindReader();

            return impl.Factory;

            void RewindReader()
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