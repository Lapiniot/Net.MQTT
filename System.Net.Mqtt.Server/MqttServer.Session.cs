using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Server.Exceptions;
using System.Security.Authentication;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer
{
#pragma warning disable CA1031 // Do not catch general exception types - method should not throw by design
    private async Task StartSessionAsync(INetworkConnection connection, CancellationToken stoppingToken)
    {
        await using(connection.ConfigureAwait(false))
        {
#pragma warning disable CA2000 // False positive from roslyn analyzer
            var transport = new NetworkConnectionAdapterTransport(connection);
#pragma warning restore CA2000
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
                                await storedContext.Completion.ConfigureAwait(false);
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
                            await currentContext.Completion.ConfigureAwait(false);
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

#pragma warning restore

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
        using var timeoutSource = new CancellationTokenSource(options.ConnectTimeout);
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
            _ = StartSessionAsync(connection, cancellationToken).ContinueWith(
                task => LogGeneralError(task.Exception), cancellationToken,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
    }
}