using System.Net.Mqtt.Packets.V5;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server.Protocol.V5;

public class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
    private MqttServerSessionState5? state;
    private Task? pingWorker;
    private CancellationTokenSource? globalCts;

    public bool CleanStart { get; init; }
    public ushort KeepAlive { get; init; }

    public MqttServerSession5(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, logger,
            observers is not null ? observers.IncomingMessage : throw new ArgumentNullException(nameof(observers)),
            true, maxUnflushedBytes) =>
        this.stateRepository = stateRepository;

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = stateRepository.GetOrCreate(ClientId, CleanStart, out var existed);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        Post(new ConnAckPacket(ConnAckPacket.Accepted, !CleanStart && existed)
        {
            RetainAvailable = false,
            SharedSubscriptionAvailable = false,
            SubscriptionIdentifiersAvailable = false
        });

        state.IsActive = true;

        globalCts = new();

        if (KeepAlive > 0)
        {
            pingWorker = RunKeepAliveMonitorAsync(TimeSpan.FromSeconds(KeepAlive * 1.5), globalCts.Token);
        }
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            globalCts?.Cancel();

            using (globalCts)
            {
                if (pingWorker is not null)
                {
                    await pingWorker.ConfigureAwait(false);
                }
            }
        }
        finally
        {
            await base.StoppingAsync().ConfigureAwait(false);
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

    protected sealed override void Dispatch(PacketType type, byte flags, in ReadOnlySequence<byte> reminder)
    {
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incuring constant number values ordered in the following way
        switch (type)
        {
            case Connect: break;
            case Publish: OnPublish(flags, in reminder); break;
            case PubAck: OnPubAck(flags, in reminder); break;
            case PubRec: OnPubRec(flags, in reminder); break;
            case PubRel: OnPubRel(flags, in reminder); break;
            case PubComp: OnPubComp(flags, in reminder); break;
            case Subscribe: OnSubscribe(flags, in reminder); break;
            case Unsubscribe: OnUnsubscribe(flags, in reminder); break;
            case PingReq: OnPingReq(); break;
            case Disconnect: OnDisconnect(); break;
            case Auth: OnAuth(flags, in reminder); break;
            default: MqttPacketHelpers.ThrowUnexpectedType((byte)type); break;
        }
    }

    private void OnPublish(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
    private void OnPubAck(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
    private void OnPubRec(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
    private void OnPubRel(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
    private void OnPubComp(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
    private void OnSubscribe(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
    private void OnUnsubscribe(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPingReq() => Post(PacketFlags.PingRespPacket);

    private void OnDisconnect()
    {
        DisconnectReceived = true;
        StopAsync().Observe();
    }

    private void OnAuth(byte flags, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();

    protected override void OnPacketReceived(byte packetType, int totalLength) => DisconnectPending = false;

    protected override void OnPacketSent(byte packetType, int totalLength)
    {
    }

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
}