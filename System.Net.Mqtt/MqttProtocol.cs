using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;

using static System.Buffers.MemoryPool<byte>;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttProtocol : MqttBinaryStreamConsumer
    {
        private ChannelReader<(MqttPacket Packet, byte[] Buffer, TaskCompletionSource Completion)> reader;
        private ChannelWriter<(MqttPacket Packet, byte[] Buffer, TaskCompletionSource Completion)> writer;
        private Task queueProcessor;
        private readonly WorkerLoop worker;

        protected MqttProtocol(NetworkTransport transport) : base(transport?.Reader)
        {
            ArgumentNullException.ThrowIfNull(transport);

            Transport = transport;

            worker = new WorkerLoop(DispatchPacketAsync);
        }

        protected NetworkTransport Transport { get; }

        protected async ValueTask<ReadOnlySequence<byte>> ReadPacketAsync(CancellationToken cancellationToken)
        {
            var reader = Transport.Reader;

            var vt = MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken);

            var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

            var sequence = result.Buffer;

            reader.AdvanceTo(sequence.End);

            return sequence;
        }

        protected void Post(MqttPacket packet)
        {
            if(!writer.TryWrite((packet, null, null)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected void Post(byte[] buffer)
        {
            if(!writer.TryWrite((null, buffer, null)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource();

            if(!writer.TryWrite((packet, null, completion)))
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

        protected async Task DispatchPacketAsync(CancellationToken cancellationToken)
        {
            var rvt = reader.ReadAsync(cancellationToken);
            var (packet, buffer, completion) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

            try
            {
                if(completion != null && completion.Task.IsCompleted) return;

                if(packet is not null)
                {
                    var total = packet.GetSize(out var remainingLength);

                    using(var memory = Shared.Rent(total))
                    {
                        packet.Write(memory.Memory.Span, remainingLength);
                        var svt = Transport.SendAsync(memory.Memory[..total], cancellationToken);
                        if(!svt.IsCompletedSuccessfully)
                        {
                            await svt.ConfigureAwait(false);
                        }
                    }
                }
                else if(buffer is not null)
                {
                    var svt = Transport.SendAsync(buffer, cancellationToken);
                    if(!svt.IsCompletedSuccessfully)
                    {
                        await svt.ConfigureAwait(false);
                    }
                }

                completion?.TrySetResult();
                OnPacketSent();
            }
            catch(Exception exception)
            {
                completion?.TrySetException(exception);
                throw;
            }
        }

        protected abstract void OnPacketSent();

        protected override Task StartingAsync(CancellationToken cancellationToken)
        {
            (reader, writer) = Channel.CreateUnbounded<(MqttPacket Packet, byte[] Buffer, TaskCompletionSource Completion)>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            queueProcessor = worker.RunAsync(default);
            return base.StartingAsync(cancellationToken);
        }

        protected override async Task StoppingAsync()
        {
            writer.Complete();

            try
            {
                await queueProcessor.ConfigureAwait(false);
            }
            catch(ChannelClosedException)
            {
                // Expected case
            }

            await worker.StopAsync().ConfigureAwait(false);

            reader = null;
            writer = null;
            queueProcessor = null;

            await base.StoppingAsync().ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            await using(worker.ConfigureAwait(false))
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}