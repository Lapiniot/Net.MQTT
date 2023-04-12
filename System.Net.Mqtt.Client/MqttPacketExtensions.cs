﻿using System.Net.Mqtt.Client.Exceptions;
using System.Net.Mqtt.Packets.V3;
using static System.Net.Mqtt.Packets.V3.ConnAckPacket;

namespace System.Net.Mqtt.Client;

internal static class MqttPacketExtensions
{
    public static void EnsureSuccessStatusCode(this ConnAckPacket packet)
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