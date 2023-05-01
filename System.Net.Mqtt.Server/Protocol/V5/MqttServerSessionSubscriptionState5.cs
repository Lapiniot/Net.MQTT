using System.Net.Mqtt.Server.Protocol.V3;

namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionSubscriptionState5 : MqttServerSessionSubscriptionState3
{
    protected override byte AddFilter(byte[] filter, byte options)
    {
        var qosLevel = (byte)(options & 0b11);
        return TryAdd(filter, qosLevel) ? qosLevel : (byte)0x80;
    }
}