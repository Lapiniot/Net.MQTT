using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;

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
            if(ConnectPacket.TryParse(buffer, out var packet))
            {
                consumed = 0;

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
    }
}