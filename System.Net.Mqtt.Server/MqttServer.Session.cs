using System.IO;
using System.IO.Pipelines;
using System.Net.Listeners;
using System.Net.Mqtt.Extensions;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private async Task StartSessionAsync(INetworkTransport connection, CancellationToken cancellationToken)
        {
            var (session, reader) = await CreateSessionAsync(connection, cancellationToken).ConfigureAwait(false);

            var clientId = session.ClientId;

            var newCookie = (Connection: connection, Session: session, Task: new Lazy<Task>(() => RunSessionAsync(connection, reader, session, cancellationToken)));

            var current = connections.GetOrAdd(clientId, newCookie);

            if(current.Connection == connection)
            {
                // Optimistic branch: there were no active session with same clientId before, we should start it now
                await current.Task.Value.ConfigureAwait(false);
            }
            else
            {
                // Pessimistic branch: there was already session running/pending, we should cancel it before attempting to run current
                await current.Connection.DisconnectAsync().ConfigureAwait(false);
                // Wait pending session task to complete
                await current.Task.Value.ConfigureAwait(false);
                // Attempt to schedule current task one more time
                if(connections.TryAdd(clientId, newCookie))
                {
                    await newCookie.Task.Value.ConfigureAwait(false);
                }
                else
                {
                    await using(session)
                    await using(reader)
                    await using(connection) {}
                }
            }
        }

        private async Task RunSessionAsync(INetworkTransport connection, NetworkPipeProducer reader, MqttServerSession session, CancellationToken cancellationToken)
        {
            var clientId = session.ClientId;

            try
            {
                await using(session)
                await using(reader)
                await using(connection)
                {
                    await session.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    LogInfo($"Client '{clientId}' connected over {connection}.");

                    await session.Completion.ConfigureAwait(false);

                    LogInfo($"Session complete for '{clientId}' on {connection}. Disconnecting...");
                }

                LogInfo($"Client '{clientId}' on {connection} disconnected.");
            }
            catch(Exception exception)
            {
                LogError(exception, $"Client '{clientId}' on {connection} terminated abnormally.");
            }
            finally
            {
                connections.TryRemove(session.ClientId, out _);
            }
        }

        private async Task<(MqttServerSession, NetworkPipeProducer)> CreateSessionAsync(INetworkTransport connection, CancellationToken cancellationToken)
        {
            using var timeoutSource = new CancellationTokenSource(connectTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);
            var token = linkedSource.Token;

            var reader = new NetworkPipeProducer(connection);

            try
            {
                await reader.ConnectAsync(token).ConfigureAwait(false);

                var hub = await DetectProtocolAsync(reader, token).ConfigureAwait(false);

                var session = hub.CreateSession(this, connection, reader);

                try
                {
                    await session.AcceptConnectionAsync(token).ConfigureAwait(false);
                }
                catch(Exception exception)
                {
                    LogError(exception, $"Error accepting connection for client '{session.ClientId}'");
                    session.Dispose();
                    throw;
                }

                return (session, reader);
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }

        private async Task<MqttProtocolHub> DetectProtocolAsync(PipeReader reader, CancellationToken token)
        {
            var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, token).ConfigureAwait(false);

            if((flags & TypeMask) != (byte)Connect) throw new InvalidDataException(ConnectPacketExpected);

            if(!buffer.Slice(offset).TryReadMqttString(out var protocol, out var consumed) ||
               string.IsNullOrEmpty(protocol))
            {
                throw new InvalidDataException(ProtocolNameExpected);
            }

            if(!buffer.Slice(offset + consumed).TryReadByte(out var level))
            {
                throw new InvalidDataException(ProtocolVersionExpected);
            }

            if(!protocolHubs.TryGetValue(level, out var hub) || hub == null) throw new InvalidDataException(NotSupportedProtocol);

            // Notify that we have not consumed any data from the pipe and 
            // cancel current pending Read operation to unblock any further 
            // immediate reads. Otherwise next reader will be blocked until 
            // new portion of data is read from network socket and flushed out
            // by writer task. Essentially, this is just a simulation of "Peek"
            // operation in terms of pipelines API.
            reader.AdvanceTo(buffer.Start, buffer.End);
            reader.CancelPendingRead();

            return hub;
        }

        private async Task StartAcceptingClientsAsync(AsyncConnectionListener listener, CancellationToken cancellationToken)
        {
            await foreach(var connection in listener.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                try
                {
                    Logger.LogInformation("Network connection accepted by {0} <=> {1}", listener, connection);
                    var _ = StartSessionAsync(connection, cancellationToken);
                }
                catch(Exception exception)
                {
                    connection?.Dispose();
                    LogError(exception, $"Cannot establish MQTT session for the connection {connection}");
                }
            }
        }
    }
}