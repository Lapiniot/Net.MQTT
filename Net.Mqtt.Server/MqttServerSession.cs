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
    protected Task? DisconnectSignal { get; private set; }
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
        DisconnectSignal = RunDisconnectWatcherAsync(ConsumerCompletion, ProducerCompletion, PublisherCompletion);
    }

    protected override async Task StoppingAsync()
    {
        Abort();

        if (pingWorker is not null)
            await pingWorker.ConfigureAwait(SuppressThrowing);
        // Suppress throwing exceptions from following tasks:
        await PublisherCompletion!.ConfigureAwait(SuppressThrowing);
        await base.StoppingAsync().ConfigureAwait(SuppressThrowing);
        // and better await on DisconnectSignal task which is already completed 
        // to rethrow potential unhandled exception which caused session to terminate.
        await DisconnectSignal!.ConfigureAwait(false);
    }

    protected abstract Task RunMessagePublisherAsync(CancellationToken stoppingToken);

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartActivityAsync(stoppingToken).ConfigureAwait(false);
            using (stoppingToken.UnsafeRegister(static state => ((MqttServerSession)state!).Disconnect(DisconnectReason.ServerShuttingDown), this))
            {
                await DisconnectSignal!.ConfigureAwait(false);
            }
        }
        finally
        {
            await StopActivityAsync().ConfigureAwait(false);
        }
    }
}