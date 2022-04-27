using System.Net.Mqtt.Client.Exceptions;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Client;

internal static class MqttPacketExtensions
{
    public static void EnsureSuccessStatusCode(this ConnAckPacket packet)
    {
        switch (packet.StatusCode)
        {
            case Accepted: return;
            case ProtocolRejected: throw new MqttInvalidProtocolVersionException();
            case IdentifierRejected: throw new MqttInvalidIdentifierException();
            case ServerUnavailable: throw new MqttServerUnavailableException();
            case CredentialsRejected: throw new MqttInvalidUserCredentialsException();
            case NotAuthorized: throw new MqttNotAuthorizedException();
            default: throw new MqttConnectionException();
        }
    }
}