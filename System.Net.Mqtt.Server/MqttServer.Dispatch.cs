using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IObserver<IncomingMessage>, IObserver<SubscriptionRequest>, IObserver<PacketReceivedMessage>
{
    //TODO: Consider using regular Dictionary<K,V> with locks to improve memory allocation during enumeration
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, Message> retainedMessages;

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
            logger.LogIncomingMessage(clientId, UTF8.GetString(topic.Span), payload.Length, qos, retain);
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
                ReadOnlySpan<byte> filterSpan = filter;

                foreach (var (topic, message) in retainedMessages)
                {
                    if (!MqttExtensions.TopicMatches(topic.Span, filterSpan))
                    {
                        continue;
                    }

                    var qosLevel = message.QoSLevel;
                    var adjustedQoS = Math.Min(qos, qosLevel);

                    request.State.OutgoingWriter.TryWrite(adjustedQoS == qosLevel ? message : message with { QoSLevel = adjustedQoS });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    #endregion

    #region Implementation of IObserver<PacketReceivedMessage>

    void IObserver<PacketReceivedMessage>.OnCompleted() { }

    void IObserver<PacketReceivedMessage>.OnError(Exception error) { }

    void IObserver<PacketReceivedMessage>.OnNext(PacketReceivedMessage value) => UpdatePacketMetrics(value.PacketType, value.TotalLength);

    #endregion

    partial void UpdatePacketMetrics(byte packetType, int totalLength);
}