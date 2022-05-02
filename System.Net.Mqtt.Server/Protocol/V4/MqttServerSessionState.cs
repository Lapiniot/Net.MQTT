﻿namespace System.Net.Mqtt.Server.Protocol.V4;

public sealed class MqttServerSessionState : V3.MqttServerSessionState
{
    public MqttServerSessionState(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, createdAt, maxInFlight)
    { }

    protected override byte AddFilter(byte[] filter, byte qosLevel) => TryAdd(filter, qosLevel) ? qosLevel : (byte)0x80;
}