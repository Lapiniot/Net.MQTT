using System.Collections.Concurrent;
using System.Linq;
using System.Net.Mqtt.Packets;
using static System.Math;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.Server
{
    internal partial class MqttSession
    {
        private readonly ConcurrentDictionary<string, byte> subscriptions;

        void IMqttPacketServerHandler.OnSubscribe(SubscribePacket packet)
        {
            var result = new byte[packet.Topics.Count];

            for(var i = 0; i < packet.Topics.Count; i++)
            {
                var (topic, qos) = packet.Topics[i];

                result[i] = IsValidTopic(topic)
                    ? subscriptions.AddOrUpdate(topic, (byte)qos, (_, __) => (byte)qos)
                    : (byte)0x80;
            }

            handler.SendSubAckAsync(packet.Id, result);
        }

        void IMqttPacketServerHandler.OnUnsubscribe(UnsubscribePacket packet)
        {
            foreach(var topic in packet.Topics)
            {
                subscriptions.TryRemove(topic, out _);
            }

            handler.SendUnsubAckAsync(packet.Id);
        }

        internal bool IsInterested(string topic, out QoSLevel qosLevel)
        {
            var topQoS = subscriptions
                .Where(s => Matches(topic, s.Key))
                .Aggregate(-1, (acc, current) => Max(acc, current.Value));

            qosLevel = (QoSLevel)topQoS;
            return topQoS != -1;
        }
    }
}