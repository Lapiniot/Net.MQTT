using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IObserver<MessageRequest>, IObserver<SubscriptionRequest>
{
    private readonly ChannelReader<Message> dispatchQueueReader;
    private readonly ChannelWriter<Message> dispatchQueueWriter;
    private readonly ConcurrentDictionary<string, Message> retainedMessages;

    #region Implementation of IObserver<MessageRequest>

    void IObserver<MessageRequest>.OnCompleted() { }

    void IObserver<MessageRequest>.OnError(Exception error) { }

    async void IObserver<MessageRequest>.OnNext(MessageRequest value)
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

        logger.LogIncomingMessage(clientId, topic, payload.Length, qos, retain);
    }

    #endregion

    #region Implementation of IObserver<SubscriptionRequest>

    void IObserver<SubscriptionRequest>.OnCompleted() { }

    void IObserver<SubscriptionRequest>.OnError(Exception error) { }

    void IObserver<SubscriptionRequest>.OnNext(SubscriptionRequest request)
    {
        foreach(var (filter, qos) in request.Filters)
        {
            Parallel.ForEach(retainedMessages, (p, s) =>
            {
                var (topic, _) = p;

                if(!MqttExtensions.TopicMatches(topic, filter)) return;

                var adjustedQoS = Math.Min(qos, p.Value.QoSLevel);
                var msg = adjustedQoS == p.Value.QoSLevel ? p.Value : new Message(topic, p.Value.Payload, adjustedQoS, true);

#pragma warning disable CA2012 // Use ValueTasks correctly
                _ = request.State.EnqueueAsync(msg, CancellationToken.None);
#pragma warning restore CA2012 // Use ValueTasks correctly
            });
        }
    }

    #endregion
}