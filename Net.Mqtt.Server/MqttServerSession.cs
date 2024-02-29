namespace Net.Mqtt.Server;

public abstract class MqttServerSession : MqttSession
{
    private Task? pingWorker;

    protected MqttServerSession(string clientId, NetworkTransportPipe transport, ILogger logger) : base(transport)
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

    public bool DisconnectReceived { get; protected set; }
    protected Task? Completion { get; set; }
    public Task? PublisherCompletion { get; private set; }

    public override string ToString() => $"'{ClientId}' over '{Transport}'";

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
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), Aborted);

        PublisherCompletion = RunMessagePublisherAsync(Aborted);

        Completion = WaitCompleteAsync();
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            Abort();

            try
            {
                if (pingWorker is not null)
                    await pingWorker.ConfigureAwait(SuppressThrowing);
                await PublisherCompletion!.ConfigureAwait(false);
            }
            finally
            {
                await base.StoppingAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (MalformedPacketException) { }
        catch (ProtocolErrorException) { }
        catch (PacketTooLargeException) { }
        catch (ConnectionClosedException)
        {
            // Expected here - shouldn't cause exception during termination even 
            // if connection was aborted before due to any reasons
        }
    }

    protected override async Task WaitCompleteAsync()
    {
        try
        {
            var anyOf = await Task.WhenAny(base.WaitCompleteAsync(), PublisherCompletion!).ConfigureAwait(false);
            await anyOf.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* Normal cancellation */ }
        catch
        {
            Disconnect(DisconnectReason.UnspecifiedError);
            throw;
        }
    }

    protected abstract Task RunMessagePublisherAsync(CancellationToken stoppingToken);

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartActivityAsync(stoppingToken).ConfigureAwait(false);
            using (stoppingToken.UnsafeRegister(static state => ((MqttServerSession)state!).Disconnect(DisconnectReason.ServerShuttingDown), this))
            {
                await Completion!.ConfigureAwait(false);
            }
        }
        finally
        {
            await StopActivityAsync().ConfigureAwait(false);
        }
    }
}