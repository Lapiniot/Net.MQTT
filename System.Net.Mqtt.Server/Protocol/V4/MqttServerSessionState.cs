using System.Net.Mqtt.Extensions;

namespace System.Net.Mqtt.Server.Protocol.V4;

public class MqttServerSessionState : V3.MqttServerSessionState
{
    public MqttServerSessionState(string clientId, DateTime createdAt) :
        base(clientId, createdAt)
    { }

    protected override byte AddTopicFilter(string filter, byte qos)
    {
        return MqttExtensions.IsValidTopic(filter) ? AddOrUpdateInternal(filter, qos) : (byte)0x80;
    }
}