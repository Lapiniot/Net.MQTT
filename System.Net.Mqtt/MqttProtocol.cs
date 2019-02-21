﻿using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt
{
    public abstract class MqttProtocol<TReader> : MqttBinaryStreamConsumer where TReader : PipeReader
    {
        private readonly ChannelReader<(MqttPacket packet, TaskCompletionSource<int> completion)> postQueueReader;
        private readonly ChannelWriter<(MqttPacket packet, TaskCompletionSource<int> completion)> postQueueWriter;
        private readonly WorkerLoop<object> postWorker;

        protected MqttProtocol(INetworkTransport transport, TReader reader) : base(reader)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Reader = reader;

            (postQueueReader, postQueueWriter) = Channel.CreateUnbounded<(MqttPacket packet, TaskCompletionSource<int> completion)>(
                new UnboundedChannelOptions {SingleReader = true});

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
                var buffer = data.GetBytes();
                var svt = Transport.SendAsync(buffer, cancellationToken);
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