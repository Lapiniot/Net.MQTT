using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private readonly ChannelReader<Message> dispatchQueueReader;
        private readonly ChannelWriter<Message> dispatchQueueWriter;
        private readonly ConcurrentDictionary<string, Message> retainedMessages;

        async void IMqttServer.OnMessage(Message message, string clientId)
        {
            var (topic, payload, qos, retain) = message;

            if(retain)
            {     
                if(payload.Length == 0)
                {
                    retainedMessages.TryRemove(topic, out _);   
                }
                else
                {
                    retainedMessages.AddOrUpdate(topic, message, (_, __) => message);
                }
            }

            await dispatchQueueWriter.WriteAsync(message).ConfigureAwait(false);

            TraceIncomingMessage(clientId, topic, payload, qos, retain);
        }

        void IMqttServer.OnSubscribe(MqttServerSessionState state, (string filter, byte qosLevel)[] filters)
        {
            foreach(var (filter, qos) in filters)
            {
                Parallel.ForEach(retainedMessages, (p, s) =>
                {
                    var (topic, _) = p;

                    if(!MqttExtensions.TopicMatches(topic, filter)) return;

                    var adjustedQoS = Math.Min(qos, p.Value.QoSLevel);
                    var msg = adjustedQoS == p.Value.QoSLevel ? p.Value : new Message(topic, p.Value.Payload, adjustedQoS, true);

#pragma warning disable CA2012 // Use ValueTasks correctly
                    _ = state.EnqueueAsync(msg);
#pragma warning restore CA2012 // Use ValueTasks correctly
                });
            }
        }
    }
}