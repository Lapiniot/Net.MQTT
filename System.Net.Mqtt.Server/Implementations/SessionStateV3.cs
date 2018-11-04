using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.Server.Implementations
{
    public class SessionStateV3 : SessionState
    {
        private readonly IPacketIdPool idPool;
        private readonly HashSet<ushort> receivedQos2;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;
        private readonly ConcurrentDictionary<string, byte> subscriptions;

        public SessionStateV3()
        {
            subscriptions = new ConcurrentDictionary<string, byte>();
            idPool = new FastPacketIdPool();
            receivedQos2 = new HashSet<ushort>();
            resendQueue = new HashQueue<ushort, MqttPacket>();
        }

        public bool IsActive { get; set; }

        internal byte[] Subscribe((string topic, QoSLevel qosLevel)[] topics)
        {
            var length = topics.Length;

            var result = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var (topic, qos) = topics[i];

                var value = (byte)qos;
                result[i] = IsValidTopic(topic)
                    ? subscriptions.AddOrUpdate(topic, value, (t, q) => value)
                    : (byte)0x80;
            }

            return result;
        }

        public void Unsubscribe(string[] topics)
        {
            foreach(var topic in topics)
            {
                subscriptions.TryRemove(topic, out _);
            }
        }

        internal bool IsInterested(string topic, out QoSLevel qosLevel)
        {
            var topQoS = subscriptions
                .Where(s => Matches(topic, s.Key))
                .Aggregate(-1, (acc, current) => Math.Max(acc, current.Value));

            qosLevel = (QoSLevel)topQoS;
            return topQoS != -1;
        }

        public bool TryAddQoS2(ushort packetId)
        {
            return receivedQos2.Add(packetId);
        }

        public bool RemoveQoS2(ushort packetId)
        {
            return receivedQos2.Remove(packetId);
        }

        public bool RemoveFromResendQueue(ushort id)
        {
            return resendQueue.TryRemove(id, out _);
        }

        public void AddToResendQueue(ushort id, MqttPacket packet)
        {
            resendQueue.AddOrUpdate(id, packet, (k, p) => packet);
        }
    }
}