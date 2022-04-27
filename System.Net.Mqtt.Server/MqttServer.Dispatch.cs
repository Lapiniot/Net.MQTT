using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IObserver<IncomingMessage>, IObserver<SubscriptionRequest>
{
    private readonly ConcurrentDictionary<Utf8String, Message> retainedMessages;

    #region Implementation of IObserver<MessageRequest>

    void IObserver<IncomingMessage>.OnCompleted() { }

    void IObserver<IncomingMessage>.OnError(Exception error) { }

    void IObserver<IncomingMessage>.OnNext(IncomingMessage incomingMessage)
    {
        var (message, clientId) = incomingMessage;
        var (topic, payload, qos, retain) = message;

        if (retain)
        {
            if (payload.Length == 0)
            {
                retainedMessages.TryRemove(topic, out _);
            }
            else
            {
                retainedMessages.AddOrUpdate(topic,
                    static (_, state) => state,
                    static (_, _, state) => state,
                    message);
            }
        }

        foreach (var (_, hub) in hubs)
        {
            hub.DispatchMessage(message);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            LogIncomingMessage(clientId, UTF8.GetString(topic.Span), payload.Length, qos, retain);
        }
    }

    #endregion

    #region Implementation of IObserver<SubscriptionRequest>

    void IObserver<SubscriptionRequest>.OnCompleted() { }

    void IObserver<SubscriptionRequest>.OnError(Exception error) { }

    void IObserver<SubscriptionRequest>.OnNext(SubscriptionRequest request)
    {
        try
        {
            foreach (var (filter, qos) in request.Filters)
            {
                // TODO: optimize to avoid delegate allocations
                Parallel.ForEach(retainedMessages, parallelOptions, pair =>
                {
                    var (_, message) = pair;
                    var (topic, _, qosLevel, _) = message;

                    if (!MqttExtensions.TopicMatches(topic.Span, filter.Span))
                    {
                        return;
                    }

                    var adjustedQoS = Math.Min(qos, qosLevel);
                    var msg = adjustedQoS == qosLevel ? message : message with { QoSLevel = adjustedQoS };

                    request.State.OutgoingWriter.TryWrite(msg);
                });
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    #endregion
}