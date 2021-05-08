using System.Collections.Generic;
using System.IO;
using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private async Task StartOrReplaceSessionAsync(INetworkConnection connection, CancellationToken cancellationToken)
        {
            var (session, transport) = await CreateSessionAsync(connection, cancellationToken).ConfigureAwait(false);

            var clientId = session.ClientId;

            var newCookie = (Connection: connection, Session: session,
                Task: new Lazy<Task>(() => RunSessionAsync(connection, transport, session, cancellationToken)));

            var (nc, mss, task) = connections.GetOrAdd(clientId, newCookie);

            if(nc == connection)
            {
                // Optimistic branch: there were no active session with same clientId before, we should start it now
                await task.Value.ConfigureAwait(false);
            }
            else
            {
                // Pessimistic branch: there was already session running/pending, we should cancel it before attempting to run current
                try
                {
                    LogInfo($"Client '{mss.ClientId}' is already connected. Terminating existing session.");
                    await nc.DisconnectAsync().ConfigureAwait(false);
                    // Wait pending session task to complete
                    await task.Value.ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch(Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    LogError(ex, $"Error while closing connection for existing session '{mss.ClientId}'");
                }

                // Attempt to schedule current task one more time, or give up and disconnect if another session has "jumped-in" faster
                if(connections.TryAdd(clientId, newCookie))
                {
                    await newCookie.Task.Value.ConfigureAwait(false);
                }
                else
                {
                    await using(session.ConfigureAwait(false))
                    await using(transport.ConfigureAwait(false))
                    await using(connection.ConfigureAwait(false)) { }
                }
            }
        }

        private async Task RunSessionAsync(INetworkConnection connection, NetworkTransport transport, MqttServerSession session, CancellationToken cancellationToken)
        {
            var clientId = session.ClientId;

            try
            {
                await using(connection.ConfigureAwait(false))
                await using(transport.ConfigureAwait(false))
                await using(session.ConfigureAwait(false))
                {
                    await session.StartAsync(cancellationToken).ConfigureAwait(false);

                    LogInfo($"Client '{clientId}' connected over {connection}.");

                    try
                    {
                        await session.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);

                        LogInfo($"Session complete by client '{clientId}' on {connection}.");
                    }
                    catch(Exception e) when(e is OperationCanceledException || e is ConnectionAbortedException)
                    {
                        LogWarning($"Session terminated by server for client '{clientId}' on {connection}");
                    }
                }
            }
            catch(Exception exception)
            {
                LogError(exception, $"Client '{clientId}' on {connection} terminated abnormally.");
                throw;
            }
            finally
            {
                connections.TryRemove(session.ClientId, out _);
            }
        }

        private async Task<(MqttServerSession, NetworkTransport)> CreateSessionAsync(INetworkConnection connection, CancellationToken stoppingToken)
        {
            using var timeoutSource = new CancellationTokenSource(connectTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);
            var cancellationToken = linkedSource.Token;

            var transport = new NetworkConnectionAdapterTransport(connection);

            try
            {
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
                    LogError(exception, $"Error accepting connection for client '{session.ClientId}'");
                    await session.DisposeAsync().ConfigureAwait(false);
                    throw;
                }

                return (session, transport);
            }
            catch
            {
                await transport.DisconnectAsync().ConfigureAwait(false);
                throw;
            }
        }

        private async Task StartAcceptingClientsAsync(IAsyncEnumerable<INetworkConnection> listener, CancellationToken cancellationToken)
        {
            LogInfo($"Start accepting incoming connections for {listener}");

            await foreach(var connection in listener.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                try
                {
                    Logger.LogInformation("Network connection accepted by {0} <=> {1}", listener, connection);
                    var _ = StartOrReplaceSessionAsync(connection, cancellationToken);
                }
                catch(Exception exception)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                    LogError(exception, $"Cannot establish MQTT session for the connection {connection}");
                    throw;
                }
            }
        }
    }
}