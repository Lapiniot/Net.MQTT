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
    public bool DisconnectReceived { get; protected set; }
    public int ActiveSubscriptions { get; protected set; }
    protected bool DisconnectPending { get; set; }

    public override string ToString() => $"'{ClientId}' over '{Transport}'";

    public virtual void Disconnect(DisconnectReason reason)
    {
        if (reason is DisconnectReason.NormalClosure)
            StopActivityAsync().Observe();
        else
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
            await Completion.WaitAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await StopActivityAsync().ConfigureAwait(false);
        }
    }
}

public enum DisconnectReason
{
    NormalClosure,
    SessionTakeOver,
    KeepAliveTimeout,
    AdministrativeAction,
    ServerShutdown
}