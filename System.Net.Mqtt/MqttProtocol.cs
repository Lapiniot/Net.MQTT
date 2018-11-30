using System.Buffers;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttProtocol : MqttBinaryStreamProcessor
    {
        private readonly ChannelReader<Memory<byte>> postQueueReader;
        private readonly ChannelWriter<Memory<byte>> postQueueWriter;
        private readonly WorkerLoop<object> postWorker;

        protected MqttProtocol(INetworkTransport transport, NetworkPipeReader reader) : base(reader)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Reader = reader;

            var channel = Channel.CreateUnbounded<Memory<byte>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            postQueueReader = channel.Reader;
            postQueueWriter = channel.Writer;

            postWorker = new WorkerLoop<object>(DispatchPacketAsync, null);
        }

        protected NetworkPipeReader Reader { get; }
        protected INetworkTransport Transport { get; }

        protected ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(buffer, cancellationToken);
        }

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
            if(!postQueueWriter.TryWrite(packet.GetBytes()))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected void Post(byte[] packet)
        {
            if(!postQueueWriter.TryWrite(packet))
            {
                throw new InvalidOperationException(CannotAddOutgoingPacket);
            }
        }

        protected async Task DispatchPacketAsync(object arg1, CancellationToken cancellationToken)
        {
            var rvt = postQueueReader.ReadAsync(cancellationToken);
            var buffer = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

            var svt = SendAsync(buffer, cancellationToken);
            if(!svt.IsCompletedSuccessfully) await svt.ConfigureAwait(false);

            OnPacketSent();
        }

        protected abstract void OnPacketSent();

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            postWorker.Start();
            return base.OnConnectAsync(cancellationToken);
        }

        protected override Task OnDisconnectAsync()
        {
            postWorker.Stop();
            return base.OnDisconnectAsync();
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