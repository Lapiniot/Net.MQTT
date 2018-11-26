using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private readonly int parallelMatchThreshold;
        private readonly ChannelReader<Message> reader;
        private readonly ConcurrentDictionary<string, Message> retainedMessages;
        private readonly ChannelWriter<Message> writer;

        public void OnMessage(Message message)
        {
            var (topic, payload, qos, retain) = message;

            if(retain)
            {
                writer.WriteAsync(new Message(topic, payload, qos, false));

                if(payload.Length == 0)
                {
                    retainedMessages.TryRemove(topic, out _);
                }
                else
                {
                    retainedMessages.AddOrUpdate(topic, message, (_, __) => message);
                }
            }
            else
            {
                writer.WriteAsync(message);
            }
        }

        public void OnSubscribe(SessionState state, (string filter, QoSLevel qosLevel)[] filters)
        {
            foreach(var (filter, qos) in filters)
            {
                foreach(var (topic, message) in retainedMessages)
                {
                    if(Matches(topic, filter))
                    {
                        var adjustedQoS = (QoSLevel)Math.Min((byte)qos, (byte)message.QoSLevel);

                        var msg = adjustedQoS == message.QoSLevel ? message : new Message(message.Topic, message.Payload, adjustedQoS, true);

                        state.EnqueueAsync(msg);
                    }
                }
            }
        }

        private async Task DispatchMessageAsync(object state, CancellationToken cancellationToken)
        {
            var vt = reader.ReadAsync(cancellationToken);

            var message = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

            var (topic, payload, qos) = message;

            var originalQos = (byte)qos;

            Parallel.ForEach(statesV3.Values, parallelOptions, stateV3 =>
            {
                if(TopicMatches(stateV3.GetSubscriptions(), topic, out var level))
                {
                    var adjustedQoS = Math.Min(originalQos, (byte)level);

                    var msg = originalQos == adjustedQoS
                        ? message
                        : new Message(topic, payload, (QoSLevel)adjustedQoS, false);

                    var _ = stateV3.EnqueueAsync(msg);
                }
            });
        }

        public bool TopicMatches(IDictionary<string, byte> subscriptions, string topic, out QoSLevel qosLevel)
        {
            var topQoS = subscriptions.Count > parallelMatchThreshold
                ? MatchParallel(subscriptions, topic)
                : MatchSequential(subscriptions, topic);

            qosLevel = (QoSLevel)topQoS;

            return topQoS != -1;
        }

        private int MatchParallel(IDictionary<string, byte> subscriptions, string topic)
        {
            return subscriptions.AsParallel().Where(s => Matches(topic, s.Key)).Aggregate(-1, Max);
        }

        private int MatchSequential(IDictionary<string, byte> subscriptions, string topic)
        {
            return subscriptions.Where(s => Matches(topic, s.Key)).Aggregate(-1, Max);
        }

        private static int Max(int acc, KeyValuePair<string, byte> current)
        {
            return Math.Max(acc, current.Value);
        }
    }
}