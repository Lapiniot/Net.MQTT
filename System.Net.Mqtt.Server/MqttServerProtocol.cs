using System.Buffers;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerProtocol : MqttProtocol
    {
        protected internal MqttServerProtocol(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
            Handlers[0x01] = OnConnectAsync;
            Handlers[0x03] = OnPublishAsync;
            Handlers[0x04] = OnPubAckAsync;
            Handlers[0x05] = OnPubRecAsync;
            Handlers[0x06] = OnPubRelAsync;
            Handlers[0x07] = OnPubCompAsync;
            Handlers[0x08] = OnSubscribeAsync;
            Handlers[0x0A] = OnUnsubscribeAsync;
            Handlers[0x0C] = OnPingReqAsync;
            Handlers[0x0E] = OnDisconnectAsync;
        }

        protected abstract ValueTask<int> OnConnectAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPublishAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubAckAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubRecAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubRelAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPubCompAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnSubscribeAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnUnsubscribeAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnPingReqAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract ValueTask<int> OnDisconnectAsync(ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);
    }
}