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

        protected abstract Task OnConAckAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPublishAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubAckAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubRecAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubRelAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPubCompAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnSubAckAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnUnsubAckAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);

        protected abstract Task OnPingRespAsync(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);
    }
}