namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState4;

public sealed class MqttServerSessionSubscriptionState4V1 : MqttServerSessionSubscriptionState3V1
{
    protected override byte AddFilter(byte[] filter, byte qos) => TryAdd(filter, qos) ? qos : (byte)0x80;
}