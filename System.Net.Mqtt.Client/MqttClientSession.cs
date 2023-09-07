﻿using System.IO.Pipelines;
using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Client;

public abstract class MqttClientSession : MqttSession
{
    private ChannelReader<PacketDispatchBlock> reader;
    private ChannelWriter<PacketDispatchBlock> writer;

    protected internal MqttClientSession(NetworkTransportPipe transport, bool disposeTransport)
        : base(transport, disposeTransport)
    { }

    public abstract byte ProtocolLevel { get; }

    public abstract string ProtocolName { get; }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<PacketDispatchBlock>(new() { SingleReader = true, SingleWriter = false });
        return base.StartingAsync(cancellationToken);
    }

    protected override Task StoppingAsync()
    {
        writer.Complete();
        return base.StoppingAsync();
    }

    protected void Post(ConnectPacket packet)
    {
        if (!writer.TryWrite(new(packet, (byte)PacketType.CONNECT, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(SubscribePacket packet)
    {
        if (!writer.TryWrite(new(packet, (byte)PacketType.SUBSCRIBE, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(UnsubscribePacket packet)
    {
        if (!writer.TryWrite(new(packet, (byte)PacketType.UNSUBSCRIBE, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer.TryWrite(new(value)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();
                var tcs = descriptor.Completion;
                if (tcs is { Task.IsCompleted: true })
                    return;

                try
                {

                    descriptor.Descriptor.WriteTo(output, out _);

                    var result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);

                    tcs?.TrySetResult();

                    if (result.IsCompleted || result.IsCanceled)
                        return;
                }
                catch (ChannelClosedException)
                {
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    tcs?.TrySetException(ex);
                    throw;
                }
            }
        }
    }

    private readonly struct PacketDispatchBlock
    {
        public PacketDispatchBlock(uint value) => Descriptor = new(value);

        public PacketDispatchBlock(IMqttPacket packet, byte packetType, TaskCompletionSource completion)
        {
            Descriptor = new(packet, packetType);
            Completion = completion;
        }

        public PacketDispatchBlock(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags, TaskCompletionSource completion)
        {
            Descriptor = new(topic, payload, flags);
            Completion = completion;
        }

        public PacketDescriptor Descriptor { get; }
        public TaskCompletionSource Completion { get; }
    }
}