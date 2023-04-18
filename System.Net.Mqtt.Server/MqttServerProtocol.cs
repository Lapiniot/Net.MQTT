﻿namespace System.Net.Mqtt.Server;

public abstract class MqttServerProtocol : MqttProtocol
{
    private ChannelReader<DispatchBlock>? reader;
    private ChannelWriter<DispatchBlock>? writer;
    private readonly int maxUnflushedBytes;

    protected internal MqttServerProtocol(NetworkTransportPipe transport, bool disposeTransport, int maxUnflushedBytes) :
        base(transport, disposeTransport) => this.maxUnflushedBytes = maxUnflushedBytes;

    protected void Post(MqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet, null, default, 0)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(null, null, default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer!.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)))))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected abstract void OnPacketSent(byte packetType, int totalLength);

    protected sealed override async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;
        FlushResult result;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
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
                            WritePublishPacket(output, (byte)(raw & 0xff), (ushort)(raw >> 8), topic, payload, out var written);
                            OnPacketSent(0b0011, written);
                        }
                        else if (raw > 0)
                        {
                            // Simple packet 4 or 2 bytes in size
                            if ((raw & 0xFF00_0000) > 0)
                            {
                                WriteRawPacket(output, raw);
                                OnPacketSent((byte)(raw >> 28), 4);
                            }
                            else
                            {
                                WriteRawPacket(output, (ushort)raw);
                                OnPacketSent((byte)(raw >> 12), 2);
                            }
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            WriteGenericPacket(output, packet, out var packetType, out var written);
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

    protected sealed override void InitPacketDispatcher() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void CompletePacketDispatch()
    {
        writer!.TryComplete();
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

    private record struct DispatchBlock(MqttPacket? Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Buffer, uint Raw);
}