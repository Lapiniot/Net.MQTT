namespace Net.Mqtt;

public abstract class MqttSession : MqttBinaryStreamConsumer
{
    public DisconnectReason DisconnectReason { get; protected set; }
    public TimeSpan ConnectionCloseTimeout { get; private set; } = TimeSpan.FromSeconds(5);

    protected MqttSession(TransportConnection connection) : base(connection?.Input)
    {
        ArgumentNullException.ThrowIfNull(connection);
        Connection = connection;
    }

    protected TransportConnection Connection { get; }

    protected Task ProducerCompletion { get; private set; } = Task.CompletedTask;

    public void Disconnect(DisconnectReason reason)
    {
        DisconnectReason = reason;
        Abort();
    }

    /// <summary>
    /// Implements the producer loop.
    /// </summary>
    /// <param name="stoppingToken">
    /// The cancellation token to signal when the producer loop should stop.
    /// </param>
    /// <returns>
    /// A task that represents long running asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Implementors should also provide graceful termination logic in the 
    /// complementary <see cref="CompleteProducer"/> method.
    /// </remarks>
    protected abstract Task RunProducerAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Completes the producer loop gracefully in non-exceptional manner.
    /// </summary>
    protected abstract void CompleteProducer();

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
            catch (OperationCanceledException)
            {
                /* Normal cancellation */
            }
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
        using var closeTimeoutCts = new CancellationTokenSource(ConnectionCloseTimeout);
        var timeoutCancellationToken = closeTimeoutCts.Token;

        // Producer loop exited normally, likely due to connection closure initiated by other party.
        // Let consumer loop to catch up (within the timeout!) and read last packets accumulated 
        // in the pipe buffer. This is crucial to ensure all pending data is processed before, 
        // otherwise we may loose some important packets (final DISCONNECT e.g.) needed 
        // for proper session teardown.
        if (ProducerCompletion.IsCompletedSuccessfully)
        {
            await ConsumerCompletion.WaitAsync(timeoutCancellationToken).ConfigureAwait(SuppressThrowing);
        }

        try
        {
            await base.StoppingAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                CompleteProducer();
                await ProducerCompletion.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected cancellation */
            }
            finally
            {
                // Signal all pending auxiliary background tasks, that 
                // monitor abortTokenSource.Token, about cancellation
                await AbortAsync().ConfigureAwait(SuppressThrowing);

                await OnConnectionClosingAsync(timeoutCancellationToken).ConfigureAwait(SuppressThrowing);
                await CloseConnectionAsync(timeoutCancellationToken).ConfigureAwait(SuppressThrowing);
                await OnConnectionClosedAsync().ConfigureAwait(false);
            }
        }

        async Task CloseConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Try to close connection gracefully
                await Connection.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // If graceful close fails or cancellation is requested, initiate abortive closure right away
                Connection.Abort();
            }
            finally
            {
                await Connection.ConnectionClosed.ConfigureAwait(SuppressThrowing);
            }
        }
    }

    /// <summary>
    /// Called when the connection is closing.
    /// Put any additional session termination logic here to be executed right
    /// before underlying network connection closure process is initiated.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token to monitor for abortive cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    protected virtual Task OnConnectionClosingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called when the connection is closed.
    /// Put any additional session termination logic here to be executed right after 
    /// the underlying network connection is closed.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    protected virtual Task OnConnectionClosedAsync() => Task.CompletedTask;
}