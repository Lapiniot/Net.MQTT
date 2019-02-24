using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private readonly ChannelReader<Message> dispatchQueueReader;
        private readonly ChannelWriter<Message> dispatchQueueWriter;
        private readonly ConcurrentDictionary<string, Message> retainedMessages;

        public void OnMessage(Message message)
        {
            var (topic, payload, qos, retain) = message;

            if(retain)
            {
                dispatchQueueWriter.WriteAsync(new Message(topic, payload, qos, false));

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
                dispatchQueueWriter.WriteAsync(message);
            }
        }

        public void OnSubscribe(SessionState state, (string filter, byte qosLevel)[] filters)
        {
            foreach(var (filter, qos) in filters)
            {
                foreach(var (topic, message) in retainedMessages)
                {
                    if(!MqttExtensions.TopicMatches(topic, filter)) continue;

                    var adjustedQoS = Math.Min(qos, message.QoSLevel);
                    var msg = adjustedQoS == message.QoSLevel ? message : new Message(message.Topic, message.Payload, adjustedQoS, true);
                    state.EnqueueAsync(msg);
                }
            }
        }

        private async Task DispatchMessageAsync(CancellationToken cancellationToken)
        {
            var vt = dispatchQueueReader.ReadAsync(cancellationToken);

            var message = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

            Parallel.ForEach(protocols.Values, protocol => protocol.NotifyMessage(message));
        }
    }
}