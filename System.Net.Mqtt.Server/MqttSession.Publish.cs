using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mqtt.Packets;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Server
{
    internal partial class MqttSession
    {
        private readonly IPacketIdPool idPool;
        private readonly ConcurrentDictionary<ushort, bool> receivedQos2;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;

        void IMqttPacketServerHandler.OnPublish(PublishPacket packet)
        {
            switch(packet.QoSLevel)
            {
                case AtMostOnce:
                {
                    server.Dispatch(packet);
                    break;
                }
                case AtLeastOnce:
                {
                    server.Dispatch(packet);
                    handler.SendPubAckAsync(packet.Id);
                    break;
                }
                case ExactlyOnce:
                {
                    if(receivedQos2.TryAdd(packet.Id, true))
                    {
                        server.Dispatch(packet);
                    }

                    handler.SendPubRecAsync(packet.Id);
                    break;
                }
            }
        }

        #region QoS Level 1

        void IMqttPacketServerHandler.OnPubAck(ushort packetId)
        {
            if(resendQueue.TryRemove(packetId, out _))
            {
                idPool.Return(packetId);
            }
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

        #region QoS Level 2

        void IMqttPacketServerHandler.OnPubRec(ushort packetId)
        {
            var pubRelPacket = new PubRelPacket(packetId);
            resendQueue.AddOrUpdate(packetId, pubRelPacket, (_, __) => pubRelPacket);
            handler.SendPubRelAsync(packetId);
        }

        void IMqttPacketServerHandler.OnPubRel(ushort packetId)
        {
            receivedQos2.TryRemove(packetId, out _);
            handler.SendPubCompAsync(packetId);
        }

        void IMqttPacketServerHandler.OnPubComp(ushort packetId)
        {
            if(resendQueue.TryRemove(packetId, out _))
            {
                idPool.Return(packetId);
            }
        }

        #endregion
    }
}