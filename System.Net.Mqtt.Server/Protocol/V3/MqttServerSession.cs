using System.Buffers;
using System.Net.Connections.Exceptions;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession : Server.MqttServerSession
{
    private static readonly byte[] PingRespPacket = new byte[] { 0b1101_0000, 0b0000_0000 };
    private readonly ISessionStateRepository<MqttServerSessionState> repository;
    private readonly IObserver<SubscriptionRequest> subscribeObserver;
#pragma warning disable CA2213 // Disposable fields should be disposed - session state lifetime is managed by the providing ISessionStateRepository
    private readonly Worker messageWorker;
    private bool disconnectPending;
    private CancelableOperationScope pingMonitor;
    private MqttServerSessionState sessionState;
#pragma warning restore

    public MqttServerSession(string clientId, NetworkTransport transport,
        ISessionStateRepository<MqttServerSessionState> stateRepository,
        ILogger logger, IObserver<SubscriptionRequest> subscribeObserver,
        IObserver<MessageRequest> messageObserver) :
        base(clientId, transport, logger, messageObserver, false)
    {
        repository = stateRepository;
        this.subscribeObserver = subscribeObserver;
        messageWorker = new WorkerLoop(ProcessMessageAsync);
    }

    public bool CleanSession { get; init; }
    public ushort KeepAlive { get; init; }
    public Message? WillMessage { get; init; }

    protected override void OnPacketSent() { }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        sessionState = repository.GetOrCreate(ClientId, CleanSession, out var existing);

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        sessionState.IsActive = true;

        sessionState.WillMessage = WillMessage;

        if(KeepAlive > 0)
        {
            disconnectPending = true;
            pingMonitor = CancelableOperationScope.StartInScope(RunPingMonitorAsync, default);
        }

        _ = messageWorker.RunAsync(default);

        await AcknowledgeConnection(existing, cancellationToken).ConfigureAwait(false);

        foreach(var packet in sessionState.ResendPackets) Post(packet);
    }

    private async Task RunPingMonitorAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(KeepAlive * 1.5));
        while(await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if(disconnectPending)
            {
                _ = StopAsync();
                break;
            }
            disconnectPending = true;
        }
    }

    protected virtual ValueTask AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
    {
        return Transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, Accepted }, cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            if(sessionState.WillMessage.HasValue)
            {
                OnMessageReceived(sessionState.WillMessage.Value);
                sessionState.WillMessage = null;
            }

            using(pingMonitor) { }

            await messageWorker.StopAsync().ConfigureAwait(false);

            await base.StoppingAsync().ConfigureAwait(false);
        }
        catch(ConnectionAbortedException)
        {
            // Expected here - shouldn't cause exception during termination even 
            // if connection was aborted before due to any reasons
        }
        finally
        {
            if(CleanSession)
            {
                repository.Remove(ClientId);
            }
            else
            {
                sessionState.IsActive = false;
            }
        }
    }

    protected override void OnConnect(byte header, ReadOnlySequence<byte> reminder)
    {
        throw new NotSupportedException();
    }

    protected override void OnPingReq(byte header, ReadOnlySequence<byte> reminder)
    {
        Post(PingRespPacket);
    }

    protected override void OnDisconnect(byte header, ReadOnlySequence<byte> reminder)
    {
        // Graceful disconnection: no need to dispatch last will message
        sessionState.WillMessage = null;

        DisconnectReceived = true;

        _ = StopAsync();
    }

    protected override void OnPacketReceived()
    {
        disconnectPending = false;
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await using(messageWorker.ConfigureAwait(false))
        {
            try
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                if(pingMonitor is not null)
                {
                    await pingMonitor.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}