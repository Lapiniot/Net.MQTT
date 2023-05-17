using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
    private ChannelReader<DispatchBlock>? reader;
    private ChannelWriter<DispatchBlock>? writer;
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
        state = stateRepository.GetOrCreate(ClientId, CleanStart, out var existed);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        Post(new ConnAckPacket(ConnAckPacket.Accepted, !CleanStart && existed)
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
                        Post(new PublishPacket(0, 0, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            Properties = message.Properties
                        });
                        break;

                    case 1:
                    case 2:
                        var id = await state.CreateMessageDeliveryStateAsync(message, stoppingToken).ConfigureAwait(false);
                        Post(new PublishPacket(id, qos, topic, payload, retain)
                        {
                            SubscriptionIds = message.SubscriptionIds,
                            ContentType = message.ContentType,
                            PayloadFormat = message.PayloadFormat,
                            ResponseTopic = message.ResponseTopic,
                            CorrelationData = message.CorrelationData,
                            Properties = message.Properties
                        });
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

    private void Post(MqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet, default)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    private void Post(uint value)
    {
        if (!writer!.TryWrite(new(default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct DispatchBlock(MqttPacket? Packet, uint Raw);
}