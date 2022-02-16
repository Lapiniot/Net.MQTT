using System.Buffers;
using System.Net.Connections.Exceptions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession : Server.MqttServerSession
{
    private static readonly byte[] pingRespPacket = { 0b1101_0000, 0b0000_0000 };
    private readonly ISessionStateRepository<MqttServerSessionState> repository;
    private readonly IObserver<SubscriptionRequest> subscribeObserver;
    private readonly AsyncSemaphore inflightSentinel;
    private readonly int maxPublishInFlight;
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
        IObserver<IncomingMessage> messageObserver, int maxPublishInFlight) :
        base(clientId, transport, logger, messageObserver, false)
    {
        if(maxPublishInFlight is <= 0 or > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPublishInFlight), "Must be number in range [1 .. 65535]");
        }

        repository = stateRepository;
        this.subscribeObserver = subscribeObserver;
        this.maxPublishInFlight = maxPublishInFlight;
        inflightSentinel = new AsyncSemaphore(maxPublishInFlight);
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

        if(inflightSentinel.CurrentCount != maxPublishInFlight)
        {
            inflightSentinel.Release(maxPublishInFlight - inflightSentinel.CurrentCount);
        }

        globalCts = new CancellationTokenSource();
        var stoppingToken = globalCts.Token;

        if(KeepAlive > 0)
        {
            disconnectPending = true;
            pingWorker = RunPingMonitorAsync(stoppingToken);
        }

        messageWorker = RunMessagePublisherAsync(stoppingToken);

        await AcknowledgeConnection(existing, cancellationToken).ConfigureAwait(false);

        if(existing)
        {
            sessionState.DispatchPendingMessages(
                resendPubRelHandler ??= ResendPubRel,
                resendPublishHandler ??= ResendPublish);
        }
    }

    private void ResendPublish(ushort id, byte flags, string topic, in ReadOnlyMemory<byte> payload)
    {
        PostPublish(flags, id, topic, in payload);
    }

    private void ResendPubRel(ushort id)
    {
        Post(PacketFlags.PubRelPacketMask | id);
    }

    private async Task RunPingMonitorAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(KeepAlive * 1.5));
        while(await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if(Volatile.Read(ref disconnectPending))
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

            globalCts.Cancel();

            using(globalCts)
            {
                try
                {
                    if(pingWorker is not null)
                    {
                        try
                        {
                            await pingWorker.ConfigureAwait(false);
                        }
                        catch(OperationCanceledException) { }
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
                        if(messageWorker is not null)
                        {
                            try
                            {
                                await messageWorker.ConfigureAwait(false);
                            }
                            catch(OperationCanceledException) { }
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
        catch(ConnectionClosedException)
        {
            // Expected here - shouldn't cause exception during termination even 
            // if connection was aborted before due to any reasons
        }
        finally
        {
            sessionState.IsActive = false;

            if(CleanSession)
            {
                repository.Remove(ClientId);
            }
            else
            {
                sessionState.Trim();
            }
        }
    }

    protected override void OnConnect(byte header, ReadOnlySequence<byte> reminder)
    {
        throw new NotSupportedException();
    }

    protected override void OnPingReq(byte header, ReadOnlySequence<byte> reminder)
    {
        Post(pingRespPacket);
    }

    protected override void OnDisconnect(byte header, ReadOnlySequence<byte> reminder)
    {
        // Graceful disconnection: no need to dispatch last will message
        sessionState.WillMessage = null;

        DisconnectReceived = true;

        _ = StopAsync();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected override void OnPacketReceived(byte packetType, int totalLength)
    {
        disconnectPending = false;
        UpdatePacketMetrics(packetType, totalLength);
    }

    partial void UpdatePacketMetrics(byte packetType, int totalLength);

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using(globalCts)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}