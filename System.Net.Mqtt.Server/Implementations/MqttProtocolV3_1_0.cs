﻿using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    internal class MqttProtocolV3_1_0 : MqttProtocol
    {
        public MqttProtocolV3_1_0(NetworkPipeReader reader) : base(reader)
        {
        }
    }
}