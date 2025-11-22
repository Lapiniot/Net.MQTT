namespace Net.Mqtt;

public abstract class MqttSession : MqttBinaryStreamConsumer
{
    public DisconnectReason DisconnectReason { get; protected set; }

    protected MqttSession(TransportConnection connection) : base(connection?.Input)
    {
        ArgumentNullException.ThrowIfNull(connection);
        Connection = connection;
    }

    protected TransportConnection Connection { get; }

    protected Task ProducerCompletion { get; private set; }

    public void Disconnect(DisconnectReason reason)
    {
        DisconnectReason = reason;
        Abort();
    }

    protected abstract Task RunProducerAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Represents implementation specific async. watcher for session termination monitoring.
    /// </summary>
    /// <param name="tasksToWatch">Tasks to be monitored for termination.</param>
    /// <returns>
    /// Returns <see cref="Task"/> which transits to <see cref="Task.IsCompleted"/> state as soon as 
    /// disconnection is initiated according to implementation specific rules.
    /// </returns>
    protected virtual Task RunDisconnectWatcherAsync(params scoped ReadOnlySpan<Task> tasksToWatch)
    {
        return ObserveCompleted(Task.WhenAny(tasksToWatch));

        async Task ObserveCompleted(Task<Task> whenAnyCompleted)
        {
            var eitherOfCompleted = await whenAnyCompleted.ConfigureAwait(false);

            try
            {
                await eitherOfCompleted.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* Normal cancellation */ }
            catch (ConnectionClosedException) { /* Connection closed abnormally, we cannot do anything about it */ }
            catch (MalformedPacketException)
            {
                Disconnect(DisconnectReason.MalformedPacket);
            }
            catch (ProtocolErrorException)
            {
                Disconnect(DisconnectReason.ProtocolError);
            }
            catch (PacketTooLargeException)
            {
                Disconnect(DisconnectReason.PacketTooLarge);
            }
            catch
            {
                Disconnect(DisconnectReason.UnspecifiedError);
                // Rethrow here as our best effort, because further implementors 
                // may have better clue about how to deal with this specific exception
                throw;
            }
        }
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        ProducerCompletion = RunProducerAsync(Aborted);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            Abort();
            await ProducerCompletion.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* expected */ }
        finally
        {
            await base.StoppingAsync().ConfigureAwait(false);
        }
    }
}