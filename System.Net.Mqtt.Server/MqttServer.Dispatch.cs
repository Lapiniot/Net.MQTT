using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer :
    IObserver<IncomingMessage3>, IObserver<IncomingMessage5>,
    IObserver<SubscribeMessage3>, IObserver<SubscribeMessage5>, IObserver<UnsubscribeMessage>,
    IObserver<PacketRxMessage>, IObserver<PacketTxMessage>
{
    //TODO: Consider using regular Dictionary<K,V> with locks to improve memory allocation during enumeration
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, Message3> retainedMessages;

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    #region Implementation of IObserver<IncomingMessage>

    void IObserver<IncomingMessage3>.OnNext(IncomingMessage3 incomingMessage)
    {
        var (message, sender) = incomingMessage;
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

        hub3?.DispatchMessage(message);
        hub4?.DispatchMessage(message);
        hub5?.DispatchMessage(new Message5(message.Topic, message.Payload, message.QoSLevel, message.Retain));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogIncomingMessage(sender.ClientId, UTF8.GetString(topic.Span), payload.Length, qos, retain);
        }
    }

    #endregion

    #region Implementation of IObserver<IncomingMessage5>

    void IObserver<IncomingMessage5>.OnNext(IncomingMessage5 incomingMessage)
    {
        var (message5, sender) = incomingMessage;
        var (topic, payload, qos, retain) = message5;
        var message3 = new Message3(topic, payload, qos, retain);

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
                    message3);
            }
        }

        hub3?.DispatchMessage(message3);
        hub4?.DispatchMessage(message3);
        hub5?.DispatchMessage(message5);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogIncomingMessage(sender.ClientId, UTF8.GetString(topic.Span), payload.Length, qos, retain);
        }
    }

    #endregion

    #region Implementation of IObserver<SubscribeMessage3>

    void IObserver<SubscribeMessage3>.OnNext(SubscribeMessage3 request)
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

                    request.QueueWriter.TryWrite(adjustedQoS == qosLevel ? message : message with { QoSLevel = adjustedQoS });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            if (RuntimeSettings.MetricsCollectionSupport)
            {
                updateStatsSignal.TrySetResult();
            }
        }
    }

    #endregion

    #region Implementation of IObserver<SubscribeMessage5>

    void IObserver<SubscribeMessage5>.OnNext(SubscribeMessage5 request)
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

                    request.QueueWriter.TryWrite(new(message.Topic, message.Payload, Math.Min(qos, message.QoSLevel), false));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            if (RuntimeSettings.MetricsCollectionSupport)
            {
                updateStatsSignal.TrySetResult();
            }
        }
    }

    #endregion

    #region Implementation of IObserver<UnsubscribeMessage>

    void IObserver<UnsubscribeMessage>.OnNext(UnsubscribeMessage value)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            updateStatsSignal.TrySetResult();
        }
    }

    #endregion

    #region Implementation of IObserver<PacketRxMessage>

    void IObserver<PacketRxMessage>.OnNext(PacketRxMessage value)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateReceivedPacketMetrics(value.PacketType, value.TotalLength);
        }
    }

    #endregion

    #region Implementation of IObserver<PacketTxMessage>

    void IObserver<PacketTxMessage>.OnNext(PacketTxMessage value)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            UpdateSentPacketMetrics(value.PacketType, value.TotalLength);
        }
    }

    #endregion
}