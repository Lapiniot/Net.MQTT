using System.Net.Mqtt.Packets.V5;


namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
    private readonly IObserver<SubscribeMessage> subscribeObserver;
    private readonly IObserver<UnsubscribeMessage> unsubscribeObserver;
    private MqttServerSessionState5? state;
    private Task? pingWorker;
    private CancellationTokenSource? globalCts;
    private Task? messageWorker;

    public bool CleanStart { get; init; }

    public ushort KeepAlive { get; init; }

    public MqttServerSession5(string clientId, NetworkTransportPipe transport, ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, logger, observers is not null ? observers.IncomingMessage : throw new ArgumentNullException(nameof(observers)),
            true, maxUnflushedBytes)
    {
        this.stateRepository = stateRepository;
        (subscribeObserver, unsubscribeObserver, _, _, _) = observers;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = stateRepository.GetOrCreate(ClientId, CleanStart, out var existed);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        Post(new ConnAckPacket(ConnAckPacket.Accepted, !CleanStart && existed)
        {
            RetainAvailable = false,
            SharedSubscriptionAvailable = false,
            SubscriptionIdentifiersAvailable = false,
            TopicAliasMaximum = ushort.MaxValue
        });

        state.IsActive = true;

        globalCts = new();
        var stoppingToken = globalCts.Token;

        if (KeepAlive > 0)
        {
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), stoppingToken);
        }

        messageWorker = RunMessagePublisherAsync(stoppingToken);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            try
            {
                globalCts?.Cancel();

                using (globalCts)
                {
                    try
                    {
                        if (pingWorker is not null)
                        {
                            await pingWorker.ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await messageWorker!.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                await base.StoppingAsync().ConfigureAwait(false);
            }
        }
        catch (ConnectionClosedException) { }
        finally
        {
            pingWorker = null;
            messageWorker = null;

            state!.IsActive = false;

            if (CleanStart)
            {
                stateRepository.Remove(ClientId);
            }
            else
            {
                state.Trim();
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected override void OnPacketReceived(byte packetType, int totalLength) => DisconnectPending = false;

    protected override void OnPacketSent(byte packetType, int totalLength) { }

    public static MqttServerSession5 Create(ConnectPacket connectPacket, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState5> repository, ILogger logger, Observers observers, int maxUnflushedBytes)
    {
        ArgumentNullException.ThrowIfNull(connectPacket);

        var clientId = !connectPacket.ClientId.IsEmpty
            ? UTF8.GetString(connectPacket.ClientId.Span)
            : Base32.ToBase32String(CorrelationIdGenerator.GetNext());

        return new MqttServerSession5(clientId, transport, repository, logger, observers, maxUnflushedBytes)
        {
            KeepAlive = connectPacket.KeepAlive,
            CleanStart = connectPacket.CleanStart
        };
    }

    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        var reader = state!.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (topic, payload, qos, _) = message;

                switch (qos)
                {
                    case 0:
                        PostPublish(0, 0, topic, in payload);
                        break;

                    case 1:
                    case 2:
                        var flags = (byte)(qos << 1);
                        var id = await state.CreateMessageDeliveryStateAsync(flags, topic, payload, stoppingToken).ConfigureAwait(false);
                        PostPublish(flags, id, topic, in payload);
                        break;

                    default:
                        InvalidQoSException.Throw();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }
}