﻿using System.Net.Pipes;

namespace System.Net.Mqtt.Broker.Implementations
{
    internal class MqttProtocolV3_1_1 : MqttProtocol
    {
        public MqttProtocolV3_1_1(NetworkPipeReader reader) : base(reader)
        {
        }
    }
}