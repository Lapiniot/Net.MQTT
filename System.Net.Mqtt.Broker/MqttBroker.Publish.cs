using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    public sealed partial class MqttBroker
    {
        public void OnPublish(MqttBinaryProtocolHandler sender, PublishPacket packet)
        {
            throw new NotImplementedException();
        }

        public void OnPubAck(MqttBinaryProtocolHandler sender, PubAckPacket packet)
        {
            throw new NotImplementedException();
        }

        public void OnPubRec(MqttBinaryProtocolHandler sender, PubRecPacket packet)
        {
            throw new NotImplementedException();
        }

        public void OnPubRel(MqttBinaryProtocolHandler sender, PubRelPacket packet)
        {
            throw new NotImplementedException();
        }

        public void OnPubComp(MqttBinaryProtocolHandler sender, PubCompPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}