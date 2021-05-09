using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Connections;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Server.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        [SuppressMessage("Microsoft.Design", "CA1031: Do not catch general exception types")]
        private async Task StartSessionAsync(INetworkConnection connection, CancellationToken stoppingToken)
        {
            try
            {
                await using(connection)
                await using(var transport = new NetworkConnectionAdapterTransport(connection))
                {
                    try
                    {
                        await using(var session = await CreateSessionAsync(transport, stoppingToken).ConfigureAwait(false))
                        {
                            var clientId = session.ClientId;
                            var context = new ConnectionContext(connection, session, new Lazy<Task>(() => RunSessionAsync(session, stoppingToken)));
                            var (storedConnection, storedSession, storedCompletionLazy) = connections.GetOrAdd(clientId, context);

                            if(storedConnection != connection)
                            {
                                // there was already session running/pending, we should cancel it before attempting to run current
                                try
                                {
                                    await storedConnection.DisconnectAsync().ConfigureAwait(false);
                                    await storedCompletionLazy.Value.ConfigureAwait(false);
                                }
                                catch(Exception exception)
                                {
                                    logger.LogSessionReplacementError(exception, storedSession.ClientId);
                                }

                                // Attempt to schedule current task one more time, or give up if another session has "jumped-in" already
                                if(!connections.TryAdd(clientId, context))
                                {
                                    return;
                                }
                            }

                            try
                            {
                                await context.CompletionLazy.Value.ConfigureAwait(false);
                            }
                            catch(OperationCanceledException)
                            {
                                logger.LogSessionTerminatedForcibly(session);
                            }
                        }
                    }
                    catch(UnsupportedProtocolVersionException upe)
                    {
                        logger.LogProtocolVersionMismatch(transport, upe.Version);
                    }
                    catch(InvalidClientIdException)
                    {
                        logger.LogInvalidClientId(transport);
                    }
                    catch(MissingConnectPacketException)
                    {
                        logger.LogMissingConnectPacket(transport);
                    }
                    catch(Exception exception)
                    {
                        logger.LogSessionError(exception, connection);
                    }
                }
            }
            catch(Exception exception)
            {
                logger.LogGeneralError(exception);
            }
        }

        private async Task RunSessionAsync(MqttServerSession session, CancellationToken stoppingToken)
        {
            try
            {
                logger.LogSessionStarting(session);
                await session.StartAsync(stoppingToken).ConfigureAwait(false);
                logger.LogSessionStarted(session);
                await session.Completion.WaitAsync(stoppingToken).ConfigureAwait(false);
                logger.LogSessionTerminatedGracefully(session);
            }
            finally
            {
                connections.TryRemove(session.ClientId, out _);
            }
        }

        private async Task<MqttServerSession> CreateSessionAsync(NetworkTransport transport, CancellationToken stoppingToken)
        {
            using var timeoutSource = new CancellationTokenSource(connectTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);
            var cancellationToken = linkedSource.Token;

            await transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var version = await MqttExtensions.DetectProtocolVersionAsync(transport.Reader, cancellationToken).ConfigureAwait(false);

            if(!protocolHubs.TryGetValue(version, out var hub) || hub == null)
            {
                throw new UnsupportedProtocolVersionException(version);
            }

            var session = hub.CreateSession(transport, this, this);

            try
            {
                await session.AcceptConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await session.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            return session;
        }

        private async Task StartAcceptingClientsAsync(IAsyncEnumerable<INetworkConnection> listener, CancellationToken cancellationToken)
        {
            logger.LogAcceptionStarted(listener);

            await foreach(var connection in listener.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                logger.LogNetworkConnectionAccepted(listener, connection);
                _ = StartSessionAsync(connection, cancellationToken);
            }
        }
    }
}