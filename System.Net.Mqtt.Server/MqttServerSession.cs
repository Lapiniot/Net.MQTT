namespace System.Net.Mqtt.Server;

public abstract class MqttServerSession : MqttSession
{
    private Task? pingWorker;

    protected MqttServerSession(string clientId, NetworkTransportPipe transport, ILogger logger, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        ClientId = clientId;
        Logger = logger;
    }

    protected ILogger Logger { get; }
    public string ClientId { get; init; }
    public ushort KeepAlive { get; init; }
    public int ActiveSubscriptions { get; protected set; }
    protected bool DisconnectPending { get; set; }
    public DisconnectReason DisconnectReason { get; protected set; }
    public bool DisconnectReceived { get; protected set; }
    protected Task? ExecuteCompletion { get; set; }
    public Task? PublisherCompletion { get; private set; }

    public override string ToString() => $"'{ClientId}' over '{Transport}'";

    public void Disconnect(DisconnectReason reason)
    {
        DisconnectReason = reason;
        Abort();
    }

    protected async Task RunKeepAliveMonitorAsync(TimeSpan period, CancellationToken stoppingToken)
    {
        DisconnectPending = true;
        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (DisconnectPending)
            {
                Disconnect(DisconnectReason.KeepAliveTimeout);
                break;
            }

            DisconnectPending = true;
        }
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        if (KeepAlive > 0)
        {
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), Aborted);
        }

        PublisherCompletion = RunMessagePublisherAsync(Aborted);

        ExecuteCompletion = ExecuteAsync();
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            Abort();

            try
            {
                try
                {
                    if (pingWorker is not null)
                    {
                        await pingWorker.ConfigureAwait(false);
                    }
                }
                finally
                {
                    await PublisherCompletion!.ConfigureAwait(false);
                }
            }
            finally
            {
                await base.StoppingAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (MalformedPacketException) { }
        catch (ProtocolErrorException) { }
        catch (ConnectionClosedException)
        {
            // Expected here - shouldn't cause exception during termination even 
            // if connection was aborted before due to any reasons
        }
    }

    protected virtual async Task ExecuteAsync()
    {
        try
        {
            await (await Task.WhenAny(ProducerCompletion, ConsumerCompletion, PublisherCompletion!).ConfigureAwait(false)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (ConnectionClosedException)
        {
            // Connection closed abnormally, we cannot do anything about it
        }
        catch (MalformedPacketException)
        {
            Disconnect(DisconnectReason.MalformedPacket);
        }
        catch (ProtocolErrorException)
        {
            Disconnect(DisconnectReason.ProtocolError);
        }
        catch
        {
            Disconnect(DisconnectReason.UnspecifiedError);
            throw;
        }
    }

    protected abstract Task RunMessagePublisherAsync(CancellationToken stoppingToken);

    protected abstract void OnPacketSent(byte packetType, int totalLength);

    [DoesNotReturn]
    protected static void ThrowInvalidDispatchBlock() =>
        throw new InvalidOperationException(InvalidDispatchBlockData);

    [DoesNotReturn]
    protected static void ThrowCannotWriteToQueue() =>
        throw new InvalidOperationException(CannotAddOutgoingPacket);

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartActivityAsync(stoppingToken).ConfigureAwait(false);
            using (stoppingToken.UnsafeRegister(static state => ((MqttServerSession)state!).Disconnect(DisconnectReason.ServerShuttingDown), this))
            {
                await ExecuteCompletion!.ConfigureAwait(false);
            }
        }
        finally
        {
            await StopActivityAsync().ConfigureAwait(false);
        }
    }
}