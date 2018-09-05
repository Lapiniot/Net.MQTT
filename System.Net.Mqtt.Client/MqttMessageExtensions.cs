using System.Net.Mqtt.Client.Exceptions;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Client
{
    internal static class MqttMessageExtensions
    {
        public static void EnsureSuccessStatusCode(this ConnAckPacket packet)
        {
            switch(packet.StatusCode)
            {
                case 0: return;
                case 1: throw new MqttInvalidProtocolVersionException();
                case 2: throw new MqttInvalidIdentifierException();
                case 3: throw new MqttServerUnavailableException();
                case 4: throw new MqttInvalidUserCredentialsException();
                case 5: throw new MqttNotAuthorizedException();
            }
        }
    }
}