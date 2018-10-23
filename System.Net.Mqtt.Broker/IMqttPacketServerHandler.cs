using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    public interface IMqttPacketServerHandler
    {
        void OnConnect(ConnectPacket packet);
        void OnSubscribe(SubscribePacket packet);
        void OnUnsubscribe(UnsubscribePacket packet);
        void OnPublish(PublishPacket packet);
        void OnPubAck(ushort packetId);
        void OnPubRec(ushort packetId);
        void OnPubRel(ushort packetId);
        void OnPubComp(ushort packetId);
        void OnPingReq();
        void OnDisconnect();
    }
}