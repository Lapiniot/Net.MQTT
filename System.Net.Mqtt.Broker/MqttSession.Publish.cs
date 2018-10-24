using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mqtt.Packets;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Broker
{
    internal partial class MqttSession
    {
        private readonly IIdentityPool idPool;
        private readonly ConcurrentDictionary<ushort, bool> receivedQos2;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;

        void IMqttPacketServerHandler.OnPublish(PublishPacket packet)
        {
            switch(packet.QoSLevel)
            {
                case AtMostOnce:
                    broker.Dispatch(packet);
                    break;
                case AtLeastOnce:
                    handler.SendPubAckAsync(packet.Id);
                    broker.Dispatch(packet);
                    break;
                case ExactlyOnce:
                    handler.SendPubRecAsync(packet.Id);
                    if(receivedQos2.TryAdd(packet.Id, true))
                    {
                        broker.Dispatch(packet);
                    }

                    break;
            }
        }

        #region QoS Level 1

        void IMqttPacketServerHandler.OnPubAck(ushort packetId)
        {
            resendQueue.TryRemove(packetId, out _);
        }

        #endregion

        #region QoS Level 2

        void IMqttPacketServerHandler.OnPubRec(ushort packetId)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPubRel(ushort packetId)
        {
            receivedQos2.TryRemove(packetId, out _);
            handler.SendPubCompAsync(packetId);
        }

        void IMqttPacketServerHandler.OnPubComp(ushort packetId)
        {
            throw new NotImplementedException();
        }

        #endregion

        internal void Dispatch(string topic, Memory<byte> payload, QoSLevel qosLevel)
        {
            switch(qosLevel)
            {
                case AtMostOnce:
                    handler.PublishAsync(topic, payload);
                    break;
                case AtLeastOnce:
                case ExactlyOnce:
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