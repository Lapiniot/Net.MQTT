namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSessionState4 : MqttServerSessionState3
{
    public MqttServerSessionState4(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, createdAt, maxInFlight)
    { }

    protected override byte AddFilter(byte[] filter, byte options) => TryAdd(filter, options) ? options : (byte)0x80;
}