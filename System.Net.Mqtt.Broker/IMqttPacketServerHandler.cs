using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    public interface IMqttPacketServerHandler
    {
        void OnConnect(MqttBinaryProtocolHandler sender, ConnectPacket packet);
        void OnSubscribe(MqttBinaryProtocolHandler sender, SubscribePacket packet);
        void OnUnsubscribe(MqttBinaryProtocolHandler sender, UnsubscribePacket packet);
        void OnPublish(MqttBinaryProtocolHandler sender, PublishPacket packet);
        void OnPubAck(MqttBinaryProtocolHandler sender, PubAckPacket packet);
        void OnPubRec(MqttBinaryProtocolHandler sender, PubRecPacket packet);
        void OnPubRel(MqttBinaryProtocolHandler sender, PubRelPacket packet);
        void OnPubComp(MqttBinaryProtocolHandler sender, PubCompPacket packet);
        void OnPingReq(MqttBinaryProtocolHandler sender);
        void OnDisconnect(MqttBinaryProtocolHandler sender);
    }
}