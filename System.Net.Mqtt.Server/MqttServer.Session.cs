using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Server.Exceptions;
using System.Security.Authentication;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer
{
    private async Task StartSessionAsync(INetworkConnection connection, CancellationToken stoppingToken)
    {
        try
        {
            await using(connection.ConfigureAwait(false))
            {
                var transport = new NetworkConnectionAdapterTransport(connection);
                await using(transport.ConfigureAwait(false))
                {
                    try
                    {
                        var session = await CreateSessionAsync(transport, stoppingToken).ConfigureAwait(false);
                        await using(session.ConfigureAwait(false))
                        {
                            var clientId = session.ClientId;
                            var currentContext = new ConnectionSessionContext(connection, session, () => RunSessionAsync(session, stoppingToken));
                            var storedContext = connections.GetOrAdd(clientId, currentContext);

                            if(storedContext.Connection != currentContext.Connection)
                            {
                                // there was already session running/pending, we should cancel it before attempting to run current
                                try
                                {
                                    await storedContext.Connection.DisconnectAsync().ConfigureAwait(false);
                                    await storedContext;
                                }
                                catch(Exception exception)
                                {
                                    LogSessionReplacementError(exception, storedContext.Session.ClientId);
                                }

                                // Attempt to schedule current task one more time, or give up if another session has "jumped-in" already
                                if(!connections.TryAdd(clientId, currentContext))
                                {
                                    return;
                                }
                            }

                            try
                            {
                                await currentContext;
                            }
                            catch(OperationCanceledException)
                            {
                                LogSessionTerminatedForcibly(session);
                            }
                            catch(ConnectionAbortedException)
                            {
                                LogConnectionAbortedByClient(session);
                            }
                        }
                    }
                    catch(UnsupportedProtocolVersionException upe)
                    {
                        LogProtocolVersionMismatch(transport, upe.Version);
                    }
                    catch(InvalidClientIdException)
                    {
                        LogInvalidClientId(transport);
                    }
                    catch(MissingConnectPacketException)
                    {
                        LogMissingConnectPacket(transport);
                    }
                    catch(AuthenticationException)
                    {
                        LogAuthenticationFailed(transport);
                    }
                    catch(Exception exception)
                    {
                        LogSessionError(exception, connection);
                    }
                }
            }
        }
        catch(Exception exception)
        {
            LogGeneralError(exception);
        }
    }

    private async Task RunSessionAsync(MqttServerSession session, CancellationToken stoppingToken)
    {
        LogSessionStarting(session);

        try
        {
            await session.StartAsync(stoppingToken).ConfigureAwait(false);
            LogSessionStarted(session);
            await session.Completion.WaitAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            connections.TryRemove(session.ClientId, out _);
        }

        LogSessionTerminatedGracefully(session);
    }

    private async Task<MqttServerSession> CreateSessionAsync(NetworkTransport transport, CancellationToken stoppingToken)
    {
        using var timeoutSource = new CancellationTokenSource(connectTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);
        var cancellationToken = linkedSource.Token;

        await transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var version = await MqttExtensions.DetectProtocolVersionAsync(transport.Reader, cancellationToken).ConfigureAwait(false);

        return protocolHubs.TryGetValue(version, out var hub) && hub is not null
            ? await hub.AcceptConnectionAsync(transport, this, this, cancellationToken).ConfigureAwait(false)
            : throw new UnsupportedProtocolVersionException(version);
    }

    private async Task StartAcceptingClientsAsync(IAsyncEnumerable<INetworkConnection> listener, CancellationToken cancellationToken)
    {
        LogAcceptionStarted(listener);

        await foreach(var connection in listener.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            LogNetworkConnectionAccepted(listener, connection);
            _ = StartSessionAsync(connection, cancellationToken);
        }
    }
}