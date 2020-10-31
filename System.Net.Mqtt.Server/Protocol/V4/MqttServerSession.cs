using System.IO;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class MqttServerSession : V3.MqttServerSession
    {
        public MqttServerSession(INetworkConnection connection, PipeReader reader, ISessionStateRepository<V3.MqttServerSessionState> stateRepository, ILogger logger,
            IObserver<(Server.MqttServerSessionState state, (string topic, byte qosLevel)[] array)> subscribeObserver,
            IObserver<MessageRequest> messageObserver) :
            base(connection, reader, stateRepository, logger, subscribeObserver, messageObserver) {}

        #region Overrides of ServerSession

        protected override async Task OnAcceptConnectionAsync(CancellationToken cancellationToken)
        {
            var rt = ReadPacketAsync(cancellationToken);
            var sequence = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

            if(ConnectPacket.TryRead(sequence, out var packet, out _))
            {
                if(packet.ProtocolLevel != 0x04 || packet.ProtocolName != "MQTT")
                {
                    await Transport.SendAsync(new byte[] {0b0010_0000, 2, 0, ProtocolRejected}, cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(IsNullOrEmpty(packet.ClientId))
                {
                    if(!packet.CleanSession)
                    {
                        await Transport.SendAsync(new byte[] {0b0010_0000, 2, 0, IdentifierRejected}, cancellationToken).ConfigureAwait(false);
                        throw new InvalidDataException(InvalidClientIdentifier);
                    }

                    ClientId = Path.GetRandomFileName().Replace('.', '-');
                }
                else
                {
                    ClientId = packet.ClientId;
                }

                CleanSession = packet.CleanSession;
                KeepAlive = packet.KeepAlive;

                if(!IsNullOrEmpty(packet.WillTopic))
                {
                    SetWillMessage(new Message(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain));
                }
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }
        }

        protected override ValueTask<int> AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(new byte[] {0b0010_0000, 2, (byte)(existing ? 1 : 0), Accepted}, cancellationToken);
        }

        #endregion
    }
}