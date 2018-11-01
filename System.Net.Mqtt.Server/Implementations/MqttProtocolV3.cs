using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.Packets.ConnAckPacket.StatusCodes;
using static System.Net.Mqtt.Server.Properties.Strings;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttProtocolV3 : MqttProtocol
    {
        private static readonly byte[] PingRespPacket = {0xD0, 0x00};

        protected internal MqttProtocolV3(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            var valueTask = MqttPacketHelpers.ReadPacketAsync(Reader, cancellationToken);
            var r = valueTask.IsCompletedSuccessfully ? valueTask.Result : await valueTask.ConfigureAwait(false);

            if(ConnectPacketV3.TryParse(r.Buffer, false, out var packet, out var consumed))
            {
                if(packet.ProtocolLevel != ConnectPacketV3.Level)
                {
                    await SendConnAckAsync(ProtocolRejected, false, cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(string.IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await SendConnAckAsync(IdentifierRejected, false, cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(InvalidClientIdentifier);
                }

                await SendConnAckAsync(Accepted, false, cancellationToken).ConfigureAwait(false);
                Reader.AdvanceTo(r.Buffer.GetPosition(consumed));
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override bool OnConnect(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotSupportedException();
        }

        protected override bool OnConAck(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPingReq(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            // TODO: implement packet validation
            SendPingRespAsync();
            consumed = 2;
            return true;
        }

        protected override bool OnPingResp(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnDisconnect(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        public ValueTask<int> SendConnAckAsync(byte statusCode, bool sessionPresent = false,
            CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new ConnAckPacket(statusCode, sessionPresent), cancellationToken);
        }

        public ValueTask<int> SendPingRespAsync(CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(PingRespPacket, cancellationToken);
        }
    }
}