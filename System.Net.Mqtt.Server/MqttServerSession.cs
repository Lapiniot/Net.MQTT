﻿namespace System.Net.Mqtt.Server;

public abstract class MqttServerSession : MqttProtocol
{
    private readonly IObserver<IncomingMessage> messageObserver;

    protected MqttServerSession(string clientId, NetworkTransportPipe transport, ILogger logger,
        IObserver<IncomingMessage> messageObserver, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        ClientId = clientId;
        Logger = logger;
        this.messageObserver = messageObserver;
    }

    protected ILogger Logger { get; }
    public string ClientId { get; init; }
    public bool DisconnectReceived { get; protected set; }
    public int ActiveSubscriptions { get; protected set; }
    protected bool DisconnectPending { get; set; }

    protected void OnMessageReceived(Message message) => messageObserver.OnNext(new(in message, ClientId));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task StartAsync(CancellationToken cancellationToken) => StartActivityAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task StopAsync() => StopActivityAsync();

    public override string ToString() => $"'{ClientId}' over '{Transport}'";

    public async Task WaitCompletedAsync(CancellationToken cancellationToken)
    {
        await ConsumerCompletion.WaitAsync(cancellationToken).ConfigureAwait(false);
        await DispatchCompletion.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected async Task RunKeepAliveMonitorAsync(TimeSpan period, CancellationToken stoppingToken)
    {
        DisconnectPending = true;
        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (DisconnectPending)
            {
                StopAsync().Observe();
                break;
            }

            DisconnectPending = true;
        }
    }

    protected abstract void OnPacketSent(byte packetType, int totalLength);

    [DoesNotReturn]
    protected static void ThrowInvalidSubscribePacket() =>
        throw new InvalidDataException("Protocol violation, SUBSCRIBE packet should contain at least one filter/QoS pair.");

    [DoesNotReturn]
    protected static void ThrowInvalidDispatchBlock() =>
        throw new InvalidOperationException(InvalidDispatchBlockData);

    [DoesNotReturn]
    protected static void ThrowCannotWriteToQueue() =>
        throw new InvalidOperationException(CannotAddOutgoingPacket);
}