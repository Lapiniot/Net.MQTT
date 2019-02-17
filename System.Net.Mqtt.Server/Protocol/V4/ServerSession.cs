using System.IO;
using System.IO.Pipelines;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket.StatusCodes;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class ServerSession : V3.ServerSession
    {
        public ServerSession(IMqttServer server, INetworkTransport transport, PipeReader reader,
            ISessionStateRepository<SessionState> stateRepository, ILogger logger) :
            base(server, transport, reader, stateRepository, logger) {}

        protected override async Task OnAcceptConnectionAsync(CancellationToken cancellationToken)
        {
            var rt = ReadPacketAsync(cancellationToken);
            var sequence = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

            if(ConnectPacket.TryRead(sequence, out var packet, out _))
            {
                if(packet.ProtocolLevel != 0x04 || packet.ProtocolName != "MQTT")
                {
                    await Transport.SendAsync(new ConnAckPacket(ProtocolRejected).GetBytes(), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(IsNullOrEmpty(packet.ClientId))
                {
                    if(!packet.CleanSession)
                    {
                        await Transport.SendAsync(new ConnAckPacket(IdentifierRejected).GetBytes(), cancellationToken).ConfigureAwait(false);
                        throw new InvalidDataException(InvalidClientIdentifier);
                    }

                    ClientId = Path.GetRandomFileName().Replace(".", "-");
                }
                else
                {
                    ClientId = packet.ClientId;
                }

                CleanSession = packet.CleanSession;
                KeepAlive = packet.KeepAlive;

                if(!IsNullOrEmpty(packet.WillTopic))
                {
                    WillMessage = new Message(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain);
                }
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }
        }
    }
}