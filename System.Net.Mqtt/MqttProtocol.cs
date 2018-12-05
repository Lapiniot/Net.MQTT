﻿using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt
{
    public abstract class MqttProtocol<TReader> : MqttBinaryStreamConsumer where TReader : PipeReader
    {
        private readonly ChannelReader<(Memory<byte> data, TaskCompletionSource<int> completion)> postQueueReader;
        private readonly ChannelWriter<(Memory<byte> data, TaskCompletionSource<int> completion)> postQueueWriter;
        private readonly WorkerLoop<object> postWorker;

        protected MqttProtocol(INetworkTransport transport, TReader reader) : base(reader)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Reader = reader;

            var channel = Channel.CreateUnbounded<(Memory<byte> data, TaskCompletionSource<int> completion)>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });

            postQueueReader = channel.Reader;
            postQueueWriter = channel.Writer;

            postWorker = new WorkerLoop<object>(DispatchPacketAsync, null);
        }

        protected TReader Reader { get; }
        protected INetworkTransport Transport { get; }

        protected async ValueTask<ReadOnlySequence<byte>> ReadPacketAsync(CancellationToken cancellationToken)
        {
            var vt = MqttPacketHelpers.ReadPacketAsync(Reader, cancellationToken);

            var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

            var sequence = result.Buffer;

            Reader.AdvanceTo(sequence.End);

            return sequence;
        }

        protected void Post(Memory<byte> packet)
        {
            if(!postQueueWriter.TryWrite((packet, null)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected Task SendAsync(Memory<byte> packet)
        {
            var completion = new TaskCompletionSource<int>(RunContinuationsAsynchronously);

            if(!postQueueWriter.TryWrite((packet, completion)))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }

            return completion.Task;
        }

        protected Task SendAsync(Memory<byte> packet, CancellationToken cancellationToken)
        {
            return cancellationToken == default
                ? SendAsync(packet)
                : SendInternalAsync(packet, cancellationToken);
        }

        protected async Task SendInternalAsync(Memory<byte> packet, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<int>(RunContinuationsAsynchronously);

            using(cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
            {
                if(!postQueueWriter.TryWrite((packet, completion)))
                {
                    throw new InvalidOperationException(CannotAddOutgoingPacket);
                }

                await completion.Task.ConfigureAwait(false);
            }
        }

        protected async Task DispatchPacketAsync(object arg1, CancellationToken cancellationToken)
        {
            var rvt = postQueueReader.ReadAsync(cancellationToken);
            var (data, completion) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

            try
            {
                if(completion != null && completion.Task.IsCompleted) return;

                var svt = Transport.SendAsync(data, cancellationToken);
                var count = svt.IsCompletedSuccessfully ? svt.Result : await svt.AsTask().ConfigureAwait(false);
                completion?.TrySetResult(count);

                OnPacketSent();
            }
            catch(Exception exception)
            {
                completion?.TrySetException(exception);
            }
        }

        protected abstract void OnPacketSent();

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            postWorker.Start();
            return base.OnConnectAsync(cancellationToken);
        }

        protected override async Task OnDisconnectAsync()
        {
            await postWorker.StopAsync().ConfigureAwait(false);
            await base.OnDisconnectAsync().ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                using(postWorker) {}
            }

            base.Dispose(disposing);
        }
    }
}