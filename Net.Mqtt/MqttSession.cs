namespace Net.Mqtt;

public abstract class MqttSession : MqttBinaryStreamConsumer
{
    private readonly bool disposeTransport;
    public DisconnectReason DisconnectReason { get; protected set; }

    protected MqttSession(NetworkTransportPipe transport, bool disposeTransport) : base(transport?.Input)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;
    }

    protected NetworkTransportPipe Transport { get; }

    protected Task ProducerCompletion { get; private set; }

    public void Disconnect(DisconnectReason reason)
    {
        DisconnectReason = reason;
        Abort();
    }

    protected abstract Task RunProducerAsync(CancellationToken stoppingToken);

    protected virtual async Task WaitCompleteAsync()
    {
        try
        {
            var anyOf = await Task.WhenAny(ProducerCompletion, ConsumerCompletion).ConfigureAwait(false);
            await anyOf.ConfigureAwait(false);
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
            throw;
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
                await Transport.DisposeAsync().ConfigureAwait(false);
        }
    }
}