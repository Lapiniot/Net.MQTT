namespace System.Net.Mqtt;

public abstract class MqttProtocol : MqttBinaryStreamConsumer
{
    private readonly bool disposeTransport;
    private Task dispatchCompletion;

    protected MqttProtocol(NetworkTransport transport, bool disposeTransport) : base(transport?.Reader)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;
    }

    protected NetworkTransport Transport { get; }

    protected abstract Task RunPacketDispatcherAsync(CancellationToken stoppingToken);
    protected abstract void InitPacketDispatcher();
    protected abstract void CompletePacketDispatch();


    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        InitPacketDispatcher();
        dispatchCompletion = RunPacketDispatcherAsync(CancellationToken.None);
        return base.StartingAsync(cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            await base.StoppingAsync().ConfigureAwait(false);
        }
        finally
        {
            CompletePacketDispatch();
            await dispatchCompletion.ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (disposeTransport)
            {
                await Transport.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}