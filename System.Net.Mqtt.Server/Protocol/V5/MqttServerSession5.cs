using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
    private ChannelReader<PacketDispatchBlock>? reader;
    private ChannelWriter<PacketDispatchBlock>? writer;
    private MqttServerSessionState5? state;
    private Task? pingWorker;
    private CancellationTokenSource? globalCts;
    private Task? messageWorker;
    private readonly int maxUnflushedBytes;
    private readonly Dictionary<ushort, ReadOnlyMemory<byte>> aliases;

    public bool CleanStart { get; init; }

    public ushort KeepAlive { get; init; }

    /// <summary>
    /// This value indicates the highest value that the Client will accept as a Topic Alias sent by the Server. 
    /// The Client uses this value to limit the number of Topic Aliases that it is willing to hold on this Connection.
    /// </summary>
    public ushort ClientTopicAliasMaximum { get; init; }

    public required IObserver<IncomingMessage5> IncomingObserver { get; init; }

    public required IObserver<SubscribeMessage5> SubscribeObserver { get; init; }

    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }

    /// <summary>
    /// This value indicates the highest value that the Server will accept as a Topic Alias sent by the Client. 
    /// The Server uses this value to limit the number of Topic Aliases that it is willing to hold on this Connection.
    /// </summary>
    public ushort ServerTopicAliasMaximum { get; init; }

    public uint ExpiryInterval { get; init; }

    public Message5 WillMessage { get; init; }

    public uint WillDelayInterval { get; init; }

    public MqttServerSession5(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, int maxUnflushedBytes) :
        base(clientId, transport, logger, true)
    {
        this.maxUnflushedBytes = maxUnflushedBytes;
        this.stateRepository = stateRepository;
        aliases = new();
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = stateRepository.Acquire(ClientId, CleanStart, out var exists);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        Post(new ConnAckPacket(ConnAckPacket.Accepted, exists)
        {
            RetainAvailable = false,
            SharedSubscriptionAvailable = false,
            TopicAliasMaximum = ServerTopicAliasMaximum
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

            if (ExpiryInterval is 0)
            {
                stateRepository.Discard(ClientId);
            }
            else
            {
                stateRepository.Release(ClientId, ExpiryInterval is uint.MaxValue ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(ExpiryInterval));
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
}