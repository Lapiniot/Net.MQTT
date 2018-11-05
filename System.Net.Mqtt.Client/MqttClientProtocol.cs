using System.Buffers;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public abstract class MqttClientProtocol : MqttProtocol
    {
        protected internal MqttClientProtocol(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
            Handlers[0x02] = OnConAckAsync;
            Handlers[0x03] = OnPublishAsync;
            Handlers[0x04] = OnPubAckAsync;
            Handlers[0x05] = OnPubRecAsync;
            Handlers[0x06] = OnPubRelAsync;
            Handlers[0x07] = OnPubCompAsync;
            Handlers[0x09] = OnSubAckAsync;
            Handlers[0x0B] = OnUnsubAckAsync;
            Handlers[0x0D] = OnPingRespAsync;
        }

        protected abstract ValueTask<int> OnConAckAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPublishAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubAckAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubRecAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubRelAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubCompAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnSubAckAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnUnsubAckAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPingRespAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);
    }
}