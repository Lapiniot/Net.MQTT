namespace System.Net.Mqtt.Server;

public abstract class MqttServerSession : MqttProtocol
{
    protected MqttServerSession(string clientId, NetworkTransportPipe transport, ILogger logger, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        ClientId = clientId;
        Logger = logger;
    }

    protected ILogger Logger { get; }
    public string ClientId { get; init; }
    public int ActiveSubscriptions { get; protected set; }
    protected bool DisconnectPending { get; set; }
    public DisconnectReason DisconnectReason { get; private set; }
    protected Task? Completed { get; set; }

    public override string ToString() => $"'{ClientId}' over '{Transport}'";

    public virtual void Disconnect(DisconnectReason reason)
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
        Completed = WaitCompletedAsync();
    }

    protected async Task WaitCompletedAsync()
    {
        try
        {
            await (await Task.WhenAny(ProducerCompletion, ConsumerCompletion).ConfigureAwait(false)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (ConnectionClosedException)
        {
            // Connection closed abnormally, we cannot do anything about it
        }
    }

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
            await Completed!.WaitAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await StopActivityAsync().ConfigureAwait(false);
        }
    }
}

public enum DisconnectReason
{
    Unknown,
    NormalClosure,
    SessionTakeOver,
    KeepAliveTimeout,
    AdministrativeAction,
    ServerShutdown
}