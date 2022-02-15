using System.Buffers.Binary;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading.Channels;
using static System.Buffers.MemoryPool<byte>;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt;

internal record struct DispatchBlock(MqttPacket Packet, string Topic, in ReadOnlyMemory<byte> Buffer, uint Raw, TaskCompletionSource Completion);
public abstract class MqttProtocol : MqttBinaryStreamConsumer
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;
    private readonly byte[] rawBuffer = new byte[4];
    private Task dispatchCompletion;
    private readonly bool disposeTransport;

    protected MqttProtocol(NetworkTransport transport, bool disposeTransport) : base(transport?.Reader)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;
    }

    protected NetworkTransport Transport { get; }

    protected void Post(MqttPacket packet)
    {
        if(!writer.TryWrite(new(packet, null, default, 0, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void Post(byte[] bytes)
    {
        if(!writer.TryWrite(new(null, null, bytes, 0, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void Post(uint value)
    {
        if(!writer.TryWrite(new(null, null, default, value, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void PostPublish(byte flags, ushort id, string topic, in ReadOnlyMemory<byte> payload)
    {
        if(!writer.TryWrite(new(null, topic, in payload, (uint)(flags | (id << 8)), null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(RunContinuationsAsynchronously);

        if(!writer.TryWrite(new(packet, null, default, 0, completion)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }

        try
        {
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException e) when(e.CancellationToken == cancellationToken)
        {
            completion.TrySetCanceled(cancellationToken);
            throw;
        }
    }

    protected async Task SendPublishAsync(byte flags, ushort id, string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(RunContinuationsAsynchronously);

        if(!writer.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }

        try
        {
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException e) when(e.CancellationToken == cancellationToken)
        {
            completion.TrySetCanceled(cancellationToken);
            throw;
        }
    }

    protected async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (packet, topic, buffer, raw, tcs) = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                // TODO: try to profile memory allocations with ArrayPool<byte> instead of MemoryPool<byte>
                try
                {
                    if(tcs is { Task.IsCompleted: true }) return;

                    if(topic is not null)
                    {
                        // Decomposed PUBLISH packet
                        var flags = (byte)(raw & 0xff);
                        var id = (ushort)(raw >> 8);

                        var total = PublishPacket.GetSize(flags, topic, buffer, out var remainingLength);
                        using var memory = Shared.Rent(total);

                        PublishPacket.Write(memory.Memory.Span, remainingLength, flags, id, topic, buffer.Span);

                        await Transport.SendAsync(memory.Memory[..total], stoppingToken).ConfigureAwait(false);
                    }
                    else if(raw > 0)
                    {
                        // Simple packet with id (4 bytes in size exactly)
                        BinaryPrimitives.WriteUInt32BigEndian(rawBuffer, raw);
                        await Transport.SendAsync(rawBuffer, stoppingToken).ConfigureAwait(false);
                    }
                    else if(buffer is { Length: > 0 })
                    {
                        // Pre-composed buffer with complete packet data
                        await Transport.SendAsync(buffer, stoppingToken).ConfigureAwait(false);
                    }
                    else if(packet is not null)
                    {
                        // Reference to any generic packet implementation
                        var total = packet.GetSize(out var remainingLength);

                        using var memory = Shared.Rent(total);
                        packet.Write(memory.Memory.Span, remainingLength);
                        await Transport.SendAsync(memory.Memory[..total], stoppingToken).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new InvalidOperationException(InvalidDispatchBlockData);
                    }

                    tcs?.TrySetResult();
                    OnPacketSent();
                }
                catch(ConnectionClosedException cce)
                {
                    tcs?.TrySetException(cce);
                    break;
                }
                catch(Exception ex)
                {
                    tcs?.TrySetException(ex);
                    throw;
                }
            }
            catch(ChannelClosedException) { break; }
            catch(OperationCanceledException) { break; }
        }
    }

    protected abstract void OnPacketSent();

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });
        dispatchCompletion = RunPacketDispatcherAsync(CancellationToken.None);
        return base.StartingAsync(cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            await base.StoppingAsync().ConfigureAwait(false);
        }
        finally
        {
            writer.Complete();
            await dispatchCompletion.ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if(disposeTransport)
            {
                await Transport.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}