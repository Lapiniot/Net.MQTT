using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Buffers.MemoryPool<byte>;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt
{
    public abstract class MqttProtocol : MqttBinaryStreamConsumer
    {
        private readonly ChannelReader<(MqttPacket Packet, byte[] Buffer, TaskCompletionSource<int> Completion)> postQueueReader;
        private readonly ChannelWriter<(MqttPacket Packet, byte[] Buffer, TaskCompletionSource<int> Completion)> postQueueWriter;
#pragma warning disable CA2213 // Disposable fields should be disposed: Warning is wrongly emitted due to some issues with analyzer itself
        private readonly WorkerLoop postWorker;
#pragma warning disable CA2213

        protected MqttProtocol(NetworkTransport transport) : base(transport?.Reader)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));

            (postQueueReader, postQueueWriter) = Channel.CreateUnbounded<(MqttPacket Packet, byte[] Buffer, TaskCompletionSource<int> Completion)>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            postWorker = new WorkerLoop(DispatchPacketAsync);
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
            if(!postQueueWriter.TryWrite((packet, null, null)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected void Post(byte[] buffer)
        {
            if(!postQueueWriter.TryWrite((null, buffer, null)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected Task SendAsync(MqttPacket packet)
        {
            var completion = new TaskCompletionSource<int>(RunContinuationsAsynchronously);

            if(!postQueueWriter.TryWrite((packet, null, completion)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }

            return completion.Task;
        }

        protected Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            return cancellationToken == default
                ? SendAsync(packet)
                : SendInternalAsync(packet, cancellationToken);
        }

        protected async Task SendInternalAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<int>(RunContinuationsAsynchronously);

            await using(cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)).ConfigureAwait(false))
            {
                if(!postQueueWriter.TryWrite((packet, null, completion)))
                {
                    throw new InvalidOperationException(CannotAddOutgoingPacket);
                }

                await completion.Task.ConfigureAwait(false);
            }
        }

        protected async Task DispatchPacketAsync(CancellationToken cancellationToken)
        {
            var rvt = postQueueReader.ReadAsync(cancellationToken);
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

                completion?.TrySetResult(0);
                OnPacketSent();
            }
            catch(Exception exception)
            {
                completion?.TrySetException(exception);
                throw;
            }
        }

        protected abstract void OnPacketSent();

        protected override Task StartingAsync(object state, CancellationToken cancellationToken)
        {
            _ = postWorker.RunAsync(default);
            return base.StartingAsync(state, cancellationToken);
        }

        protected override async Task StoppingAsync()
        {
            await postWorker.StopAsync().ConfigureAwait(false);
            await base.StoppingAsync().ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            await using(postWorker.ConfigureAwait(false))
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}