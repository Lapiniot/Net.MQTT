using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    public interface IMqttPacketServerHandler
    {
        void OnConnect(ConnectPacket packet);
        void OnSubscribe(SubscribePacket packet);
        void OnUnsubscribe(UnsubscribePacket packet);
        void OnPublish(PublishPacket packet);
        void OnPubAck(PubAckPacket packet);
        void OnPubRec(PubRecPacket packet);
        void OnPubRel(PubRelPacket packet);
        void OnPubComp(PubCompPacket packet);
        void OnPingReq();
        void OnDisconnect();
    }
}