using Net.Mqtt.Client.Exceptions;
using Net.Mqtt.Packets.V3;
using static Net.Mqtt.Packets.V3.ConnAckPacket;

namespace Net.Mqtt.Client;

internal static class MqttPacketExtensions
{
    extension(ConnAckPacket packet)
    {
        public void EnsureSuccessStatusCode()
        {
            switch (packet.StatusCode)
            {
                case Accepted: break;
                case ProtocolRejected: MqttInvalidProtocolVersionException.Throw(); break;
                case IdentifierRejected: MqttInvalidIdentifierException.Throw(); break;
                case ServerUnavailable: MqttServerUnavailableException.Throw(); break;
                case CredentialsRejected: MqttInvalidUserCredentialsException.Throw(); break;
                case NotAuthorized: MqttNotAuthorizedException.Throw(); break;
                default: MqttConnectionException.Throw(); break;
            }
        }
    }
}