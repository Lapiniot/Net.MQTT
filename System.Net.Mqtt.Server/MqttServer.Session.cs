using System.Security.Authentication;

namespace System.Net.Mqtt.Server;

#pragma warning disable CA1031

public sealed partial class MqttServer
{
    private async Task StartSessionAsync(NetworkConnection connection, CancellationToken stoppingToken)
    {
        await using (connection.ConfigureAwait(false))
        {
#pragma warning disable CA2000
            var transport = new NetworkTransportPipe(connection);
#pragma warning restore CA2000
            await using (transport.ConfigureAwait(false))
            {
                try
                {
                    await connection.ConnectAsync(stoppingToken).ConfigureAwait(false);
                    transport.Start();
                    var session = await CreateSessionAsync(transport, stoppingToken).ConfigureAwait(false);
                    await using (session.ConfigureAwait(false))
                    {
                        var clientId = session.ClientId;
                        var pendingContext = new ConnectionSessionContext(connection, session, RunSessionAsync, stoppingToken);
                        var currentContext = connections.GetOrAdd(clientId, pendingContext);

                        if (currentContext != pendingContext)
                        {
                            // there was already session running/pending, we should cancel it before attempting to run current
                            try
                            {
                                currentContext.Abort();
                                await currentContext.RunAsync().ConfigureAwait(false);
                            }
                            catch (Exception exception)
                            {
                                LogSessionReplacementError(exception, currentContext.Session.ClientId);
                            }

                            // Attempt to schedule current task one more time, or give up if another session has "jumped-in" already
                            if (!connections.TryAdd(clientId, pendingContext))
                            {
                                return;
                            }
                        }

                        await pendingContext.RunAsync().ConfigureAwait(false);
                    }
                }
                catch (UnsupportedProtocolVersionException upe)
                {
                    LogProtocolVersionMismatch(transport, upe.Version);
                }
                catch (InvalidClientIdException)
                {
                    LogInvalidClientId(transport);
                }
                catch (MissingConnectPacketException)
                {
                    LogMissingConnectPacket(transport);
                }
                catch (AuthenticationException)
                {
                    LogAuthenticationFailed(transport);
                }
                catch (Exception exception)
                {
                    LogSessionError(exception, connection);
                }
            }
        }
    }

    private async Task RunSessionAsync(NetworkConnection connection, MqttServerSession session, CancellationToken stoppingToken)
    {
        LogSessionStarting(session);

        try
        {
            await session.StartAsync(stoppingToken).ConfigureAwait(false);
            LogSessionStarted(session);
            await session.WaitCompletedAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogSessionAbortedForcibly(session);
            return;
        }
        catch (ConnectionClosedException)
        {
            // expected
        }
        finally
        {
            connections.TryRemove(session.ClientId, out _);
            await connection.DisconnectAsync().ConfigureAwait(false);
        }

        if (session.DisconnectReceived)
        {
            LogSessionTerminatedGracefully(session);
        }
        else
        {
            LogConnectionAbortedByClient(session);
        }
    }

    private async Task<MqttServerSession> CreateSessionAsync(NetworkTransportPipe transport, CancellationToken stoppingToken)
    {
        using var timeoutSource = new CancellationTokenSource(options.ConnectTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);
        var cancellationToken = linkedSource.Token;

        var version = await DetectProtocolVersionAsync(transport.Input, cancellationToken).ConfigureAwait(false);

        if (!hubs.TryGetValue(version, out var hub) || hub is null)
        {
            UnsupportedProtocolVersionException.Throw(version);
        }

        return await hub.AcceptConnectionAsync(transport, this, this, this, cancellationToken).ConfigureAwait(false);
    }

    private async Task StartAcceptingClientsAsync(IAsyncEnumerable<NetworkConnection> listener, CancellationToken cancellationToken)
    {
        LogAcceptionStarted(listener);

        await foreach (var connection in listener.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            LogNetworkConnectionAccepted(listener, connection);
            _ = StartSessionAsync(connection, cancellationToken).ContinueWith(
                task => LogGeneralError(task.Exception), cancellationToken,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
    }

    private static async Task<int> DetectProtocolVersionAsync(PipeReader reader, CancellationToken token)
    {
        var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, token).ConfigureAwait(false);

        if ((flags & PacketFlags.TypeMask) != 0b0001_0000)
        {
            MissingConnectPacketException.Throw();
        }

        if (!SE.TryReadMqttString(buffer.Slice(offset), out var protocol, out var consumed) || protocol is not { Length: > 0 })
        {
            ThrowProtocolNameExpected();
        }

        if (!SE.TryReadByte(buffer.Slice(offset + consumed), out var level))
        {
            ThrowProtocolVersionExpected();
        }

        // Notify that we have not consumed any data from the pipe and 
        // cancel current pending Read operation to unblock any further 
        // immediate reads. Otherwise next reader will be blocked until 
        // new portion of data is read from network socket and flushed out
        // by writer task. Essentially, this is just a simulation of "Peek"
        // operation in terms of pipelines API.
        reader.AdvanceTo(buffer.Start, buffer.End);
        reader.CancelPendingRead();

        return level;
    }

    [DoesNotReturn]
    private static void ThrowProtocolNameExpected() =>
        throw new InvalidDataException("Valid MQTT protocol name is expected.");

    [DoesNotReturn]
    private static void ThrowProtocolVersionExpected() =>
        throw new InvalidDataException("Valid MQTT protocol version is expected.");
}