﻿using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<TMessage, TSessionState, TConnPacket> : MqttProtocolHub<TMessage>,
    ISessionStateRepository<TSessionState>,
    ISessionStatisticsFeature,
    IAsyncDisposable
    where TSessionState : MqttServerSessionState<TMessage>
    where TConnPacket : MqttPacket, IBinaryReader<TConnPacket>
{
    private readonly ILogger logger;
    private readonly ChannelReader<TMessage> messageQueueReader;
    private readonly ChannelWriter<TMessage> messageQueueWriter;

#pragma warning disable CA2213
    private readonly CancelableOperationScope messageWorker;
#pragma warning restore CA2213
    private readonly ConcurrentDictionary<string, TSessionState> states;
    private readonly IEnumerator<KeyValuePair<string, TSessionState>> statesEnumerator;
    private int disposed;

    protected MqttProtocolHubWithRepository(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;

        states = new();
        statesEnumerator = states.GetEnumerator();
        (messageQueueReader, messageQueueWriter) = Channel.CreateUnbounded<TMessage>(new() { SingleReader = false, SingleWriter = false });
        messageWorker = CancelableOperationScope.Start(ProcessMessageQueueAsync);
    }

    protected ILogger Logger => logger;
    public required IObserver<PacketRxMessage> PacketRxObserver { get; init; }
    public required IObserver<PacketTxMessage> PacketTxObserver { get; init; }

    private async Task ProcessMessageQueueAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await messageQueueReader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (messageQueueReader.TryRead(out var message))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    statesEnumerator.Reset();
                    while (statesEnumerator.MoveNext())
                    {
                        Dispatch(statesEnumerator.Current.Value, message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (ChannelClosedException)
        {
            // expected
        }
    }

    protected abstract void Dispatch(TSessionState sessionState, TMessage? message);

    [LoggerMessage(17, LogLevel.Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage", SkipEnabledCheck = true)]
    protected partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (statesEnumerator)
        {
            try
            {
                await using (messageWorker.ConfigureAwait(false))
                {
                    messageQueueWriter.Complete();
                }
            }
            finally
            {
                Parallel.ForEach(states, state => (state.Value as IDisposable)?.Dispose());
            }
        }
    }

    #endregion

    #region Overrides of MqttProtocolHub

    public sealed override async Task<MqttServerSession> AcceptConnectionAsync(NetworkTransportPipe transport, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var reader = transport.Input;

        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);
        var buffer = packet.Buffer;

        try
        {
            if (TConnPacket.TryRead(in buffer, out var connPacket, out var packetSize))
            {
                var (exception, connAckPacket) = Validate(connPacket);

                if (exception is null)
                {
                    return CreateSession(connPacket, transport);
                }
                else
                {
                    // Negative acknowledgment is performed by the hub itself
                    await transport.Output.WriteAsync(connAckPacket, cancellationToken).ConfigureAwait(false);
                    // Mark output as completed, since no more data will be sent
                    // and wait output worker to complete, ensuring all data is flushed to the network
                    await transport.CompleteOutputAsync().ConfigureAwait(false);
                    // Notify observers directly about Rx/Tx activity, because 
                    // session will not be created at all due to the protocol error
                    PacketRxObserver.OnNext(new((byte)PacketType.CONNECT, packetSize));
                    PacketTxObserver.OnNext(new((byte)PacketType.CONNACK, connAckPacket.Length));
                    throw exception;
                }
            }
            else
            {
                MissingConnectPacketException.Throw();
                return null;
            }
        }
        finally
        {
            reader.AdvanceTo(buffer.Start);
        }
    }

    protected abstract (Exception? Exception, ReadOnlyMemory<byte> ConnAckPacket) Validate(TConnPacket? connPacket);

    protected abstract MqttServerSession CreateSession(TConnPacket connectPacket, NetworkTransportPipe transport);

    public sealed override void DispatchMessage(TMessage message) => messageQueueWriter.TryWrite(message);

    #endregion

    #region Implementation of ISessionStateRepository<out T>

    public TSessionState GetOrCreate(string clientId, bool clean, out bool existed)
    {
        TSessionState? current, created;

        if (clean)
        {
            created = CreateState(clientId, true);

            while (true)
            {
                current = states.GetOrAdd(clientId, created);
                if (created == current)
                {
                    existed = false;
                    break;
                }
                else if (states.TryUpdate(clientId, created, current))
                {
                    (current as IDisposable)?.Dispose();
                    existed = true;
                    break;
                }
            }

            return created;
        }

        if (states.TryGetValue(clientId, out current))
        {
            existed = true;
            return current;
        }

        created = CreateState(clientId, false);
        current = states.GetOrAdd(clientId, created);

        if (current == created)
        {
            existed = false;
        }
        else
        {
            (created as IDisposable)?.Dispose();
            existed = true;
        }

        return current;
    }

    protected abstract TSessionState CreateState(string clientId, bool clean);

    public void Remove(string clientId)
    {
        states.TryRemove(clientId, out var state);
        (state as IDisposable)?.Dispose();
    }

    #endregion

    #region ISessionStatisticsFeature implementation

    public int GetTotalSessions()
    {
        var total = 0;
        foreach (var (_, state) in states)
        {
            total++;
        }

        return total;
    }

    public int GetActiveSessions()
    {
        var total = 0;
        foreach (var (_, state) in states)
        {
            if (state.IsActive) total++;
        }

        return total;
    }

    #endregion
}