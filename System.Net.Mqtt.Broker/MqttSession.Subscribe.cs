using System.Collections.Concurrent;
using System.Linq;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Broker
{
    internal partial class MqttSession
    {
        private readonly ConcurrentDictionary<string, QoSLevel> subscriptions;

        void IMqttPacketServerHandler.OnSubscribe(SubscribePacket packet)
        {
            var result = new byte[packet.Topics.Count];

            for(var i = 0; i < packet.Topics.Count; i++)
            {
                var (topic, qos) = packet.Topics[i];

                result[i] = MqttTopicHelpers.IsValidTopic(topic)
                    ? (byte)subscriptions.AddOrUpdate(topic, qos, (_, __) => qos)
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

        internal bool IsInterested(string topic)
        {
            return subscriptions.Keys.Any(filter => MqttTopicHelpers.Matches(topic, filter));
        }
    }
}