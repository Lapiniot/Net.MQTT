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
        }

        protected NetworkPipeReader Reader { get; }
        protected INetworkTransport Transport { get; }

        public ValueTask<int> SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return Transport.SendAsync(packet.GetBytes(), cancellationToken);
        }

        public ValueTask<int> SendPacketAsync(byte[] packet, CancellationToken cancellationToken = default)
        {
            return Transport.SendAsync(packet, cancellationToken);
        }
    }
}