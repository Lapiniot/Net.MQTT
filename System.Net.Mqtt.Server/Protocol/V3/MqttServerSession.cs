using System.Net.Connections.Exceptions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession : Server.MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState> repository;
    private readonly IObserver<SubscriptionRequest> subscribeObserver;
#pragma warning disable CA2213 // Disposable fields should be disposed - session state lifetime is managed by the providing ISessionStateRepository
    private MqttServerSessionState sessionState;
#pragma warning restore CA2213
    private CancellationTokenSource globalCts;
    private Task messageWorker;
    private Task pingWorker;
    private bool disconnectPending;
    private PubRelDispatchHandler resendPubRelHandler;
    private PublishDispatchHandler resendPublishHandler;

#pragma warning restore

    public MqttServerSession(string clientId, NetworkTransport transport,
        ISessionStateRepository<MqttServerSessionState> stateRepository,
        ILogger logger, IObserver<SubscriptionRequest> subscribeObserver,
        IObserver<IncomingMessage> messageObserver, int maxUnflushedBytes) :
        base(clientId, transport, logger, messageObserver, false, maxUnflushedBytes)
    {
        repository = stateRepository;
        this.subscribeObserver = subscribeObserver;
    }

    public bool CleanSession { get; init; }
    public ushort KeepAlive { get; init; }
    public Message? WillMessage { get; init; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        sessionState = repository.GetOrCreate(ClientId, CleanSession, out var existing);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        sessionState.IsActive = true;

        sessionState.WillMessage = WillMessage;

        globalCts = new();
        var stoppingToken = globalCts.Token;

        if (KeepAlive > 0)
        {
            disconnectPending = true;
            pingWorker = RunPingMonitorAsync(stoppingToken);
        }

        messageWorker = RunMessagePublisherAsync(stoppingToken);

        await AcknowledgeConnection(existing, cancellationToken).ConfigureAwait(false);

        if (existing)
        {
            sessionState.DispatchPendingMessages(
                resendPubRelHandler ??= ResendPubRel,
                resendPublishHandler ??= ResendPublish);
        }
    }

    private void ResendPublish(ushort id, byte flags, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload) =>
        PostPublish(flags, id, topic, in payload);

    private void ResendPubRel(ushort id) => Post(PacketFlags.PubRelPacketMask | id);

    private async Task RunPingMonitorAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(KeepAlive * 1.5));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (Volatile.Read(ref disconnectPending))
            {
                _ = StopAsync();
                break;
            }

            disconnectPending = true;
        }
    }

    protected virtual async ValueTask AcknowledgeConnection(bool existing, CancellationToken cancellationToken) =>
        await Transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.Accepted }, cancellationToken).ConfigureAwait(false);

    protected override async Task StoppingAsync()
    {
        try
        {
            if (sessionState.WillMessage.HasValue)
            {
                OnMessageReceived(sessionState.WillMessage.Value);
                sessionState.WillMessage = null;
            }

            globalCts.Cancel();

            using (globalCts)
            {
                try
                {
                    if (pingWorker is not null)
                    {
                        try
                        {
                            await pingWorker.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            pingWorker = null;
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (messageWorker is not null)
                        {
                            try
                            {
                                await messageWorker.ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { }
                            finally
                            {
                                messageWorker = null;
                            }
                        }
                    }
                    finally
                    {
                        await base.StoppingAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        catch (ConnectionClosedException)
        {
            // Expected here - shouldn't cause exception during termination even 
            // if connection was aborted before due to any reasons
        }
        finally
        {
            sessionState.IsActive = false;

            if (CleanSession)
            {
                repository.Remove(ClientId);
            }
            else
            {
                sessionState.Trim();
            }
        }
    }

    protected sealed override void OnConnect(byte header, ReadOnlySequence<byte> reminder) { }

    protected sealed override void OnPingReq(byte header, ReadOnlySequence<byte> reminder) => Post(PacketFlags.PingRespPacket);

    protected sealed override void OnDisconnect(byte header, ReadOnlySequence<byte> reminder)
    {
        // Graceful disconnection: no need to dispatch last will message
        sessionState.WillMessage = null;

        DisconnectReceived = true;

        _ = StopAsync();
    }

    protected sealed override void OnPacketReceived(byte packetType, int totalLength)
    {
        disconnectPending = false;
        UpdatePacketMetrics(packetType, totalLength);
    }

    partial void UpdatePacketMetrics(byte packetType, int totalLength);

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}