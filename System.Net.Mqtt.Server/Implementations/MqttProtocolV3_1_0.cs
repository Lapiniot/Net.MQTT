using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttProtocolV3_1_0 : MqttProtocol
    {
        protected internal MqttProtocolV3_1_0(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }

        protected override bool OnConnect(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(ConnectPacketV3.TryParse(buffer, out _))
            {
                consumed = 0;
                SendConnAckAsync(0, false);
                return true;
            }

            consumed = 0;
            return false;
        }

        protected override bool OnConAck(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPingReq(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPingResp(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnDisconnect(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        public ValueTask<int> SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(packet.GetBytes(), cancellationToken);
        }

        public ValueTask<int> SendPacketAsync(byte[] packet, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(packet, cancellationToken);
        }

        public ValueTask<int> SendConnAckAsync(byte statusCode, bool sessionPresent, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new ConnAckPacket(statusCode, sessionPresent), cancellationToken);
        }
    }
}