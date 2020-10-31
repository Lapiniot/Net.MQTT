using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer : IObserver<(Message Message, string ClientId)>, IObserver<(MqttServerSessionState State, (string topic, byte qosLevel)[] Filters)>
    {
        private readonly ChannelReader<Message> dispatchQueueReader;
        private readonly ChannelWriter<Message> dispatchQueueWriter;
        private readonly ConcurrentDictionary<string, Message> retainedMessages;

        #region Implementation of IObserver<in (Message Message, string ClientId)>

        void IObserver<(Message Message, string ClientId)>.OnCompleted() {}

        void IObserver<(Message Message, string ClientId)>.OnError(Exception error) {}

        async void IObserver<(Message Message, string ClientId)>.OnNext((Message Message, string ClientId) value)
        {
            var ((topic, payload, qos, retain), clientId) = value;

            if(retain)
            {
                if(payload.Length == 0)
                {
                    retainedMessages.TryRemove(topic, out _);
                }
                else
                {
                    retainedMessages.AddOrUpdate(topic, value.Message, (_, __) => value.Message);
                }
            }

            await dispatchQueueWriter.WriteAsync(value.Message).ConfigureAwait(false);

            TraceIncomingMessage(clientId, topic, payload, qos, retain);
        }

        #endregion

        #region Implementation of IObserver<(MqttServerSessionState state, (string topic, byte qosLevel)[] array)>

        void IObserver<(MqttServerSessionState State, (string topic, byte qosLevel)[] Filters)>.OnCompleted() {}

        void IObserver<(MqttServerSessionState State, (string topic, byte qosLevel)[] Filters)>.OnError(Exception error) {}

        void IObserver<(MqttServerSessionState State, (string topic, byte qosLevel)[] Filters)>.OnNext((MqttServerSessionState State, (string topic, byte qosLevel)[] Filters) value)
        {
            foreach(var (filter, qos) in value.Filters)
            {
                Parallel.ForEach(retainedMessages, (p, s) =>
                {
                    var (topic, _) = p;

                    if(!MqttExtensions.TopicMatches(topic, filter)) return;

                    var adjustedQoS = Math.Min(qos, p.Value.QoSLevel);
                    var msg = adjustedQoS == p.Value.QoSLevel ? p.Value : new Message(topic, p.Value.Payload, adjustedQoS, true);

#pragma warning disable CA2012 // Use ValueTasks correctly
                    _ = value.State.EnqueueAsync(msg);
#pragma warning restore CA2012 // Use ValueTasks correctly
                });
            }
        }

        #endregion
    }
}