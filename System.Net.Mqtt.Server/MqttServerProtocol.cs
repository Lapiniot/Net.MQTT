using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server;

public abstract class MqttServerProtocol : MqttProtocol
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;
    private readonly int maxUnflushedBytes;

    protected internal MqttServerProtocol(NetworkTransportPipe transport, bool disposeTransport, int maxUnflushedBytes) :
        base(transport, disposeTransport)
    {
        this[Connect] = OnConnect;
        this[Subscribe] = OnSubscribe;
        this[Unsubscribe] = OnUnsubscribe;
        this[PingReq] = OnPingReq;
        this[Disconnect] = OnDisconnect;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    protected abstract void OnConnect(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnSubscribe(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnUnsubscribe(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnPingReq(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnDisconnect(byte header, in ReadOnlySequence<byte> reminder);

    protected void Post(MqttPacket packet)
    {
        if (!writer.TryWrite(new(packet, null, default, 0)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer.TryWrite(new(null, null, default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)))))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected sealed override async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;
        FlushResult result;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var block))
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    var (packet, topic, payload, raw) = block;

                    try
                    {
                        if (!topic.IsEmpty)
                        {
                            // Decomposed PUBLISH packet
                            WritePublishPacket(output, (byte)(raw & 0xff), (ushort)(raw >> 8), topic, payload);
                        }
                        else if (raw > 0)
                        {
                            // Simple packet 4 or 2 bytes in size
                            WriteRawPacket(output, raw);
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            WriteGenericPacket(output, packet);
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

    protected sealed override void InitPacketDispatcher() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void CompletePacketDispatch()
    {
        writer.TryComplete();
        Transport.Output.CancelPendingFlush();
    }

    [DoesNotReturn]
    protected static void ThrowInvalidSubscribePacket() =>
        throw new InvalidDataException("Protocol violation, SUBSCRIBE packet should contain at least one filter/QoS pair.");

    [DoesNotReturn]
    protected static void ThrowInvalidDispatchBlock() =>
        throw new InvalidOperationException(InvalidDispatchBlockData);

    [DoesNotReturn]
    protected static void ThrowCannotWriteToQueue() =>
        throw new InvalidOperationException(CannotAddOutgoingPacket);

    private record struct DispatchBlock(MqttPacket Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Buffer, uint Raw);
}