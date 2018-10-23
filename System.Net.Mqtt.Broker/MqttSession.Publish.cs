using System.Collections.Generic;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    internal partial class MqttSession
    {
        private readonly IIdentityPool idPool;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;

        void IMqttPacketServerHandler.OnPubAck(PubAckPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPubComp(PubCompPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPublish(PublishPacket packet)
        {
            broker.Dispatch(packet);
        }

        void IMqttPacketServerHandler.OnPubRec(PubRecPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPubRel(PubRelPacket packet)
        {
            throw new NotImplementedException();
        }

        internal void Dispatch(string topic, Memory<byte> payload, QoSLevel qosLevel)
        {
            switch(qosLevel)
            {
                case QoSLevel.AtMostOnce:
                    handler.PublishAsync(topic, payload);
                    break;
                case QoSLevel.AtLeastOnce:
                case QoSLevel.ExactlyOnce:
                {
                    var id = idPool.Rent();
                    var packet = new PublishPacket(id, qosLevel, topic, payload);
                    if(resendQueue.TryAdd(id, packet))
                    {
                        handler.PublishAsync(packet);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(qosLevel), qosLevel, null);
            }
        }
    }
}