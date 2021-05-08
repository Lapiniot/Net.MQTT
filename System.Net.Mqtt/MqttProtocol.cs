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
        private readonly ChannelReader<(MqttPacket packet, TaskCompletionSource<int> completion)> postQueueReader;
        private readonly ChannelWriter<(MqttPacket packet, TaskCompletionSource<int> completion)> postQueueWriter;
        private readonly WorkerLoop postWorker;

        protected MqttProtocol(NetworkTransport transport) : base(transport?.Reader)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));

            (postQueueReader, postQueueWriter) = Channel.CreateUnbounded<(MqttPacket packet, TaskCompletionSource<int> completion)>(
                new UnboundedChannelOptions { SingleReader = true });

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
            if(!postQueueWriter.TryWrite((packet, null)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected Task SendAsync(MqttPacket packet)
        {
            var completion = new TaskCompletionSource<int>(RunContinuationsAsynchronously);

            if(!postQueueWriter.TryWrite((packet, completion)))
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
                if(!postQueueWriter.TryWrite((packet, completion)))
                {
                    throw new InvalidOperationException(CannotAddOutgoingPacket);
                }

                await completion.Task.ConfigureAwait(false);
            }
        }

        protected async Task DispatchPacketAsync(CancellationToken cancellationToken)
        {
            var rvt = postQueueReader.ReadAsync(cancellationToken);
            var (packet, completion) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

            try
            {
                if(completion != null && completion.Task.IsCompleted) return;

                var total = packet.GetSize(out var remainingLength);

                using(var buffer = Shared.Rent(total))
                {
                    packet.Write(buffer.Memory.Span, remainingLength);
                    var svt = Transport.SendAsync(buffer.Memory.Slice(0, total), cancellationToken);
                    var count = svt.IsCompletedSuccessfully ? svt.Result : await svt.AsTask().ConfigureAwait(false);
                    completion?.TrySetResult(count);
                }

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
            _ = postWorker.RunAsync(default);
            return base.StartingAsync(cancellationToken);
        }

        protected override async Task StoppingAsync()
        {
            await postWorker.StopAsync().ConfigureAwait(false);
            await base.StoppingAsync().ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await postWorker.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}