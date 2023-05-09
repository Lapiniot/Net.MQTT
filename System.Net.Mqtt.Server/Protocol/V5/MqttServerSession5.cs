using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
    private readonly IObserver<SubscribeMessage> subscribeObserver;
    private readonly IObserver<UnsubscribeMessage> unsubscribeObserver;
    private ChannelReader<DispatchBlock>? reader;
    private ChannelWriter<DispatchBlock>? writer;
    private MqttServerSessionState5? state;
    private Task? pingWorker;
    private CancellationTokenSource? globalCts;
    private Task? messageWorker;
    private readonly int maxUnflushedBytes;

    public bool CleanStart { get; init; }

    public ushort KeepAlive { get; init; }

    public MqttServerSession5(string clientId, NetworkTransportPipe transport, ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, logger, observers is not null ? observers.IncomingMessage : throw new ArgumentNullException(nameof(observers)), true)
    {
        this.maxUnflushedBytes = maxUnflushedBytes;
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

                var (topic, payload, qos, retain) = message;

                switch (qos)
                {
                    case 0:
                        Post(new PublishPacket(0, 0, topic, payload, retain));
                        break;

                    case 1:
                    case 2:
                        var flags = (byte)(qos << 1);
                        var id = await state.CreateMessageDeliveryStateAsync(flags, topic, payload, stoppingToken).ConfigureAwait(false);
                        Post(new PublishPacket(id, qos, topic, payload, retain));
                        break;

                    default:
                        InvalidQoSException.Throw();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }

    protected sealed override async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
    {
        FlushResult result;
        var output = Transport.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var block))
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    var (packet, raw) = block;

                    try
                    {
                        if (raw > 0)
                        {
                            // Simple packet 4 or 2 bytes in size
                            if ((raw & 0xFF00_0000) > 0)
                            {
                                WritePacket(output, raw);
                                OnPacketSent((byte)(raw >> 28), 4);
                            }
                            else
                            {
                                WritePacket(output, (ushort)raw);
                                OnPacketSent((byte)(raw >> 12), 2);
                            }
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            WritePacket(output, packet, out var packetType, out var written);
                            OnPacketSent(packetType, written);
                        }
                        else
                        {
                            ThrowInvalidDispatchBlock();
                        }

                        if (output.UnflushedBytes > maxUnflushedBytes)
                        {
                            result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                            if (result.IsCompleted || result.IsCanceled)
                                return;
                        }
                    }
                    catch (ConnectionClosedException)
                    {
                        break;
                    }
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
                return;
        }
    }

    protected sealed override void OnPacketDispatcherStartup() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void OnPacketDispatcherShutdown()
    {
        writer!.TryComplete();
        Transport.Output.CancelPendingFlush();
    }

    protected void Post(MqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet, default)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    private record struct DispatchBlock(MqttPacket? Packet, uint Raw);
}