using System.Net.Connections.Exceptions;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server;

public abstract class MqttServerProtocol : MqttProtocol
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;

    protected internal MqttServerProtocol(NetworkTransport transport, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        this[Connect] = OnConnect;
        this[Subscribe] = OnSubscribe;
        this[Unsubscribe] = OnUnsubscribe;
        this[PingReq] = OnPingReq;
        this[Disconnect] = OnDisconnect;
    }

    protected abstract void OnConnect(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnSubscribe(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPingReq(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnDisconnect(byte header, ReadOnlySequence<byte> reminder);

    protected void Post(MqttPacket packet)
    {
        if (!writer.TryWrite(new(packet, null, default, 0)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(byte[] bytes)
    {
        if (!writer.TryWrite(new(null, null, bytes, 0)))
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
                            var flags = (byte)(raw & 0xff);
                            var id = (ushort)(raw >> 8);

                            var total = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
                            var buffer = output.GetMemory(total);

                            PublishPacket.Write(buffer.Span, remainingLength, flags, id, topic.Span, payload.Span);
                            output.Advance(total);
                            await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                        }
                        else if (raw > 0)
                        {
                            // Simple packet with id (4 bytes in size exactly)
                            var buffer = output.GetMemory(4);
                            BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, raw);
                            output.Advance(4);
                            await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                        }
                        else if (payload is { Length: > 0 })
                        {
                            /*
                            TODO: Seems this branch is only used for 2-byte length PingResp packets, 
                            consider switch to simple bit-packing into DispatchBlock.Raw field 
                            */

                            // Pre-composed buffer with complete packet data
                            await output.WriteAsync(payload, stoppingToken).ConfigureAwait(false);
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            var total = packet.GetSize(out var remainingLength);
                            var buffer = output.GetMemory(total);

                            packet.Write(buffer.Span, remainingLength);
                            output.Advance(total);
                            await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                        }
                        else
                        {
                            ThrowInvalidDispatchBlock();
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
        }
    }

    protected sealed override void InitPacketDispatcher() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void CompletePacketDispatch() => writer.Complete();

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