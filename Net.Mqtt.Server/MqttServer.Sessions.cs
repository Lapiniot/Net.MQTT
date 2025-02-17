using System.Security.Authentication;
using SequenceExtensions = Net.Mqtt.Extensions.SequenceExtensions;

namespace Net.Mqtt.Server;

#pragma warning disable CA1031

public sealed partial class MqttServer
{
    private async Task RunSessionAsync(TransportConnection connection, CancellationToken stoppingToken)
    {
        await Task.CompletedTask.ConfigureAwait(ForceYielding);

        await using (connection.ConfigureAwait(false))
        {
            try
            {
                await connection.StartAsync(stoppingToken).ConfigureAwait(false);
                var session = await CreateSessionAsync(connection, stoppingToken).ConfigureAwait(false);
                await using (session.ConfigureAwait(false))
                {
                    var clientId = session.ClientId;
                    var context = new ConnectionSessionContext(connection, session, logger, DateTime.UtcNow, stoppingToken);

                    // Retry until we win optimistic lock race
                    while (true)
                    {
                        var current = connections.GetOrAdd(clientId, context);

                        if (context == current)
                        {
                            break;
                        }

                        // there was already session running/pending, we should cancel it before attempting to run current
                        // as far as MQTT protocol dictates exactly this behavior for already existing active sessions
                        try
                        {
                            current.Session.Disconnect(DisconnectReason.SessionTakenOver);
                            await current.RunAsync().WaitAsync(stoppingToken).ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            logger.LogSessionTakeoverError(exception, current.Session.ClientId);
                        }

                        if (connections.TryUpdate(clientId, context, current))
                        {
                            break;
                        }
                    }

                    try
                    {
                        // Ensure session has been already started before connection 
                        // state notification dispatch to maintain internal consistency
                        var task = context.RunAsync();
                        if (!task.IsCompleted)
                            NotifyConnected(session);
                        await task.ConfigureAwait(false);
                    }
                    finally
                    {
                        // Atomically check both key and value before removing, so only currently running session is swept!
                        connections.TryRemove(new(clientId, context));
                        NotifyDisconnected(context.Session);
                    }
                }
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken != stoppingToken)
            {
                logger.LogConnectTimeout(connection);
            }
            catch (UnsupportedProtocolVersionException upe)
            {
                logger.LogProtocolVersionMismatch(connection, upe.Version);
            }
            catch (InvalidClientIdException)
            {
                logger.LogInvalidClientId(connection);
            }
            catch (MissingConnectPacketException)
            {
                logger.LogMissingConnectPacket(connection);
            }
            catch (AuthenticationException)
            {
                logger.LogAuthenticationFailed(connection);
            }
            catch (Exception exception)
            {
                logger.LogSessionError(exception, connection);
            }
        }
    }

    private void NotifyConnected(MqttServerSession session) =>
        connStateMessageQueue.Writer.TryWrite(new(ConnectionStatus.Connected, session.ClientId));

    private void NotifyDisconnected(MqttServerSession session) =>
        connStateMessageQueue.Writer.TryWrite(new(ConnectionStatus.Disconnected, session.ClientId));

    private async Task<MqttServerSession> CreateSessionAsync(TransportConnection connection, CancellationToken stoppingToken)
    {
        using var timeoutSource = new CancellationTokenSource(options.ConnectTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);
        var cancellationToken = linkedSource.Token;

        try
        {
            var version = await DetectProtocolVersionAsync(connection.Input, cancellationToken).ConfigureAwait(false);

            MqttServerSessionFactory? factory = version switch
            {
                3 => hub3,
                4 => hub4,
                5 => hub5,
                _ => null
            };

            if (factory is null)
            {
                UnsupportedProtocolVersionException.Throw(version);
            }

            return await factory.AcceptConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (RuntimeOptions.MetricsCollectionSupported)
            {
                Interlocked.Increment(ref rejectedConnections);
            }

            await connection.CompleteOutputAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task AcceptConnectionsAsync(IAsyncEnumerable<TransportConnection> listener, CancellationToken cancellationToken)
    {
        logger.LogAcceptionStarted(listener);

        await foreach (var connection in listener.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            logger.LogNetworkConnectionAccepted(listener, connection);
            if (RuntimeOptions.MetricsCollectionSupported)
            {
                Interlocked.Increment(ref totalConnections);
            }

            RunSessionAsync(connection, cancellationToken).Observe(LogError);
        }

        void LogError(Exception e) => logger.LogGeneralError(e);
    }

    private static async Task<int> DetectProtocolVersionAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);

        if ((flags & PacketFlags.TypeMask) != 0b0001_0000)
        {
            MissingConnectPacketException.Throw();
        }

        if (!SequenceExtensions.TryReadMqttString(buffer.Slice(offset), out var protocol, out var consumed) || protocol is not { Length: > 0 })
        {
            MalformedPacketException.Throw("CONNECT");
        }

        if (!SequenceExtensions.TryRead(buffer.Slice(offset + consumed), out var level))
        {
            MalformedPacketException.Throw("CONNECT");
        }

        reader.AdvanceTo(buffer.Start);
        return level;
    }
}