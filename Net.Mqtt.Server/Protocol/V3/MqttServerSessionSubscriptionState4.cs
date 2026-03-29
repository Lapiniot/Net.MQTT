namespace Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSessionSubscriptionState4 : MqttServerSessionSubscriptionState3
{
    protected override byte GetReturnCode(bool valid, byte qos) => valid ? qos : (byte)0x80;
}