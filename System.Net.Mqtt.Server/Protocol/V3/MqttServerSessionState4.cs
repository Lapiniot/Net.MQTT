namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSessionState4 : MqttServerSessionState3
{
    public MqttServerSessionState4(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, createdAt, maxInFlight)
    { }

    protected override byte AddFilter(byte[] filter, byte qosLevel) => TryAdd(filter, qosLevel) ? qosLevel : (byte)0x80;
}