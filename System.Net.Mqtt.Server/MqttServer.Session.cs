using System.Collections.Generic;
using System.IO;
using System.Net.Connections;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private async Task StartOrReplaceSessionAsync(INetworkConnection connection, CancellationToken stoppingToken)
        {
            await using(connection)
            await using(var transport = new NetworkConnectionAdapterTransport(connection))
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
#pragma warning disable CA1031 // Do not catch general exception types
                    catch(Exception exception)
                    {
                        logger.LogSessionReplacementError(exception, storedSession.ClientId);
                    }
#pragma warning restore CA1031 // Do not catch general exception types

                    // Attempt to schedule current task one more time, or give up if another session has "jumped-in" already
                    if(!connections.TryAdd(clientId, context))
                    {
                        return;
                    }
                }

                // there were no active session with same clientId before, we should start it now
                await context.CompletionLazy.Value.ConfigureAwait(false);
            }
        }

        private async Task RunSessionAsync(MqttServerSession session, CancellationToken stoppingToken)
        {
            try
            {
                await session.StartAsync(stoppingToken).ConfigureAwait(false);
                await session.Completion.WaitAsync(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                connections.TryRemove(session.ClientId, out _);
            }
        }

        private async Task<MqttServerSession> CreateSessionAsync(NetworkConnectionAdapterTransport transport, CancellationToken stoppingToken)
        {
            using var timeoutSource = new CancellationTokenSource(connectTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);
            var cancellationToken = linkedSource.Token;

            await transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var version = await MqttExtensions.DetectProtocolVersionAsync(transport.Reader, cancellationToken).ConfigureAwait(false);

            if(!protocolHubs.TryGetValue(version, out var hub) || hub == null)
            {
                throw new InvalidDataException(NotSupportedProtocol);
            }

            var session = hub.CreateSession(transport, this, this);

            try
            {
                await session.AcceptConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch(Exception exception)
            {
                logger.LogConnectionRejectedError(exception, session.ClientId);
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
                try
                {
                    logger.LogNetworkConnectionAccepted(listener, connection);
                    var _ = StartOrReplaceSessionAsync(connection, cancellationToken);
                }
                catch(Exception exception)
                {
                    logger.LogSessionRejectedError(exception, connection);
                    await connection.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
            }
        }
    }
}