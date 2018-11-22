using System.IO;
using System.IO.Pipelines;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Server.Properties.Strings;

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

                var _ = RunSessionAsync(connection, cancellationToken)
                    .ContinueWith((task, state) => ((INetworkTransport)state).Dispose(),
                        connection, cancellationToken);
            }
            catch(Exception exception)
            {
                connection?.Dispose();
                TraceError(exception);
            }
        }

        private async Task RunSessionAsync(INetworkTransport connection, CancellationToken cancellationToken)
        {
            var session = await CreateSessionAsync(connection, cancellationToken).ConfigureAwait(false);

            activeSessions.AddOrUpdate(session.ClientId, session, (clientId, existing) =>
            {
                existing.CloseSessionAsync();
                return session;
            });

            try
            {
                await session.ConnectAsync(cancellationToken).ConfigureAwait(false);

                await session.Completion.ConfigureAwait(false);
            }
            finally
            {
                activeSessions.TryRemove(session.ClientId, out _);

                session.Dispose();
            }
        }

        private async Task<MqttServerSession> CreateSessionAsync(INetworkTransport connection, CancellationToken cancellationToken)
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

                    var session = factory(connection, reader);

                    try
                    {
                        await session.AcceptConnectionAsync(token).ConfigureAwait(false);
                    }
                    catch
                    {
                        session?.Dispose();
                        throw;
                    }

                    return session;
                }
                catch
                {
                    reader.Dispose();
                    throw;
                }
            }
        }

        private async Task<ServerSessionFactory> DetectProtocolAsync(PipeReader reader, CancellationToken token)
        {
            var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, token).ConfigureAwait(false);

            if((flags & TypeMask) != (byte)Connect) throw new InvalidDataException(ConnectPacketExpected);

            if(!MqttHelpers.TryReadString(buffer.Slice(offset), out var protocol, out var consumed) ||
               string.IsNullOrEmpty(protocol))
            {
                throw new InvalidDataException(ProtocolNameExpected);
            }

            if(!MqttHelpers.TryReadByte(buffer.Slice(offset + consumed), out var level))
            {
                throw new InvalidDataException(ProtocolVersionExpected);
            }

            var index = level - 0x03;

            var factory = index < protocols.Length ? protocols[index] : null;

            if(factory == null) throw new InvalidDataException(NotSupportedProtocol);

            // Notify that we have not consumed any data from the pipe and 
            // cancel current pending Read operation to unblock any further 
            // immediate reads. Otherwise next reader will be blocked until 
            // new portion of data is read from network socket and flushed out
            // by writer task. Essentially, this is just a simulation of "Peek"
            // operation in terms of pipelines API.
            reader.AdvanceTo(buffer.Start, buffer.End);
            reader.CancelPendingRead();

            return factory;
        }
    }
}