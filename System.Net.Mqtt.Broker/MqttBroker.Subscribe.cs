using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    public sealed partial class MqttBroker
    {
        public void OnSubscribe(MqttBinaryProtocolHandler sender, SubscribePacket packet)
        {
            throw new NotImplementedException();
        }

        public void OnUnsubscribe(MqttBinaryProtocolHandler sender, UnsubscribePacket packet)
        {
            throw new NotImplementedException();
        }
    }
}