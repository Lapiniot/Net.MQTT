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
    public partial class MqttServerSessionV3 : MqttServerSession<SessionStateV3>
    {
        private static readonly byte[] PingRespPacket = {0xD0, 0x00};
        private SessionStateV3 state;

        public MqttServerSessionV3(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionStateV3> stateProvider) :
            base(transport, reader, stateProvider)
        {
        }

        public bool CleanSession { get; set; }

        public string ClientId { get; set; }


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

                CleanSession = packet.CleanSession;
                ClientId = packet.ClientId;

                if(CleanSession)
                {
                    StateProvider.Remove(ClientId);
                    state = StateProvider.Create(ClientId);
                }
                else
                {
                    state = StateProvider.Get(ClientId) ?? StateProvider.Create(ClientId);
                }
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

        protected override bool OnPingReq(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            // TODO: implement packet validation
            SendPacketAsync(PingRespPacket, default);
            consumed = 2;
            return true;
        }

        protected override bool OnDisconnect(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 2;

            if(CleanSession)
            {
                StateProvider.Remove(ClientId);
            }

            state.IsActive = false;

            Transport.DisconnectAsync();

            return true;
        }

        public ValueTask<int> SendConnAckAsync(byte statusCode, bool sessionPresent = false,
            CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new ConnAckPacket(statusCode, sessionPresent), cancellationToken);
        }
    }
}