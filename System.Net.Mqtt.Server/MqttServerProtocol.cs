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

        protected abstract Task OnConnectAsync(byte header, ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);

        protected abstract Task OnPublishAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubAckAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubRecAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubRelAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubCompAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnSubscribeAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnUnsubscribeAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPingReqAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnDisconnectAsync(byte header, ReadOnlySequence<byte> readOnlySequence, CancellationToken cancellationToken);
    }
}