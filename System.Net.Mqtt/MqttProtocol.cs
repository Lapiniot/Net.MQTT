using System.Buffers;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public abstract class MqttProtocol : MqttBinaryStreamProcessor
    {
        protected MqttProtocol(INetworkTransport transport, NetworkPipeReader reader) : base(reader)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Reader = reader;
            Reader.OnWriterCompleted(OnWriterCompleted, null);
        }

        private void OnWriterCompleted(Exception exception, object state)
        {
            OnStreamCompleted(exception);
        }

        protected abstract void OnStreamCompleted(Exception exception);

        protected NetworkPipeReader Reader { get; }
        protected INetworkTransport Transport { get; }

        protected ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(buffer, cancellationToken);
        }

        protected ValueTask<int> SendAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(packet.GetBytes(), cancellationToken);
        }

        protected async ValueTask<ReadOnlySequence<byte>> ReadPacketAsync(CancellationToken cancellationToken)
        {
            var vt = MqttPacketHelpers.ReadPacketAsync(Reader, cancellationToken);

            var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

            var sequence = result.Buffer;

            Reader.AdvanceTo(sequence.End);

            return sequence;
        }
    }
}