
namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSessionSubscriptionState4 : MqttServerSessionSubscriptionState3
{
    protected override byte AddFilter(byte[] filter, byte qos) => TryAdd(filter, qos) ? qos : (byte)0x80;
}