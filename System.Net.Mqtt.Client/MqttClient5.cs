using System.Collections.Concurrent;
using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Client;

public sealed partial class MqttClient5 : MqttClient
{
    private ChannelReader<PacketDescriptor> reader;
    private ChannelWriter<PacketDescriptor> writer;
    private readonly ChannelReader<MqttMessage> incomingQueueReader;
    private readonly ChannelWriter<MqttMessage> incomingQueueWriter;
    private readonly NetworkConnection connection;
    private readonly MqttConnectionOptions5 connectionOptions;
    private bool connectionAcknowledged;
    private TaskCompletionSource connAckTcs;
    private CancellationTokenSource globalCts;
    private Task pingCompletion;
    private Task messageNotifierCompletion;
    private MqttSessionState<Message5> sessionState;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

    public MqttClient5(NetworkConnection connection, string clientId, int maxInFlight, bool disposeTransport) :
#pragma warning disable CA2000
        base(clientId, new NetworkTransportPipe(connection), disposeTransport)
#pragma warning restore CA2000
    {
        Verify.ThrowIfNotInRange(maxInFlight, 1, ushort.MaxValue);

        this.connection = connection;
        this.maxInFlight = maxInFlight;
        connectionOptions = MqttConnectionOptions5.Default;
        pendingCompletions = new();
        (incomingQueueReader, incomingQueueWriter) = Channel.CreateUnbounded<MqttMessage>(new() { SingleReader = true, SingleWriter = true });
    }

    public ushort KeepAlive { get; private set; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<PacketDescriptor>(new() { SingleReader = true, SingleWriter = false });
        connectionAcknowledged = false;
        receivedIncompleteQoS2 = 0;
        ReceiveMaximum = connectionOptions.ReceiveMaximum;
        MaxReceivePacketSize = connectionOptions.MaxPacketSize;
        MaxSendPacketSize = int.MaxValue;
        connAckTcs?.TrySetCanceled(default);
        connAckTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        globalCts?.Dispose();
        globalCts = new();

        await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        Transport.Reset();
        Transport.Start();

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        var connPacket = new ConnectPacket(ClientId is { } ? UTF8.GetBytes(ClientId) : default,
            keepAlive: connectionOptions.KeepAlive, cleanStart: connectionOptions.CleanStart,
            connectionOptions is { UserName: { } uname } ? UTF8.GetBytes(uname) : default,
            connectionOptions is { Password: { } pwd } ? UTF8.GetBytes(pwd) : default,
            connectionOptions is { LastWillTopic: { } lw } ? UTF8.GetBytes(lw) : default,
            connectionOptions.LastWillMessage, (byte)connectionOptions.LastWillQoS, connectionOptions.LastWillRetain)
        {
            ReceiveMaximum = ReceiveMaximum,
            MaximumPacketSize = (uint)MaxReceivePacketSize
        };

        Post(connPacket);
    }

    protected override async Task StoppingAsync()
    {
        writer.Complete();
        Parallel.ForEach(pendingCompletions, c => c.Value.TrySetCanceled());
        pendingCompletions.Clear();
        globalCts.Cancel();

        try
        {
            try
            {
                if (pingCompletion is not null)
                {
                    await pingCompletion.ConfigureAwait(false);
                    pingCompletion = null;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                try
                {
                    await messageNotifierCompletion.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }
        finally
        {
            await base.StoppingAsync().ConfigureAwait(false);

            try
            {
                await Transport.Output.WriteAsync(new byte[] { 0b1110_0000, 0 }, default).ConfigureAwait(false);
                await Transport.CompleteOutputAsync().ConfigureAwait(false);
            }
            finally
            {
                await connection.DisconnectAsync().ConfigureAwait(false);
                await Transport.StopAsync().ConfigureAwait(false);
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        using (globalCts)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task StartPingWorkerAsync(TimeSpan period, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            Post(PacketFlags.PingReqPacket);
        }
    }

    private async Task StartMessageNotifierAsync(CancellationToken stoppingToken)
    {
        while (await incomingQueueReader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (incomingQueueReader.TryRead(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();
                OnMessageReceived(message);
            }
        }
    }

    private void AcknowledgePacket(ushort packetId, object result = null)
    {
        if (pendingCompletions.TryGetValue(packetId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }

    private void ResendPublish(ushort id, in Message5 message)
    {
        if (!message.Topic.IsEmpty)
        {
            Post(new PublishPacket(id, message.QoSLevel, message.Topic, message.Payload, message.Retain, duplicate: true)
            {
                SubscriptionIds = message.SubscriptionIds,
                ContentType = message.ContentType,
                PayloadFormat = message.PayloadFormat,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                UserProperties = message.UserProperties
            });
        }
        else
        {
            Post(PacketFlags.PubRelPacketMask | id);
        }

        OnMessageDeliveryStarted();
    }

    public override async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
    {
        if (!connectionAcknowledged)
        {
            await connAckTcs.Task.ConfigureAwait(false);
        }

        var acknowledgeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var packetId = sessionState.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(new SubscribePacket(packetId, topics.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.qos)).ToArray()));
            return await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as byte[];
        }
        catch (OperationCanceledException)
        {
            acknowledgeTcs.TrySetCanceled(cancellationToken);
            throw;
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out var tcs);
            sessionState.ReturnId(packetId);
        }
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        if (!connectionAcknowledged)
        {
            await connAckTcs.Task.ConfigureAwait(false);
        }

        var acknowledgeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var packetId = sessionState.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(new UnsubscribePacket(packetId, topics.Select(t => (ReadOnlyMemory<byte>)UTF8.GetBytes(t)).ToArray()));
            await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            acknowledgeTcs.TrySetCanceled(cancellationToken);
            throw;
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }

    public override async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, QoSLevel qosLevel = QoSLevel.QoS0, bool retain = false, CancellationToken cancellationToken = default)
    {
        var qos = (byte)qosLevel;
        var topicBytes = UTF8.GetBytes(topic);
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (qos is 0)
        {
            Post(new PublishPacket(0, qos, topicBytes, payload, retain), completionSource);
        }
        else
        {
            if (!connectionAcknowledged)
            {
                await connAckTcs.Task.ConfigureAwait(false);
            }

            await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
            var id = sessionState.CreateMessageDeliveryState(new(topicBytes, payload, qos, retain));
            Post(new PublishPacket(id, qos, topicBytes, payload, retain), completionSource);
            OnMessageDeliveryStarted();
        }

        await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CompleteMessageDelivery(ushort id)
    {
        if (sessionState.DiscardMessageDeliveryState(id))
        {
            OnMessageDeliveryComplete();
            inflightSentinel.TryRelease(1);
        }
    }

    private readonly record struct PacketDescriptor(IMqttPacket5 Packet, uint Raw, TaskCompletionSource Completion);
}