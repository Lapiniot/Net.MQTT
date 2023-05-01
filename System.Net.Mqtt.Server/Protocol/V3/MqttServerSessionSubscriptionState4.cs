
namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionSubscriptionState4 : MqttServerSessionSubscriptionState3
{
    protected override byte AddFilter(byte[] filter, byte options) => TryAdd(filter, options) ? options : (byte)0x80;
}