using System.Collections.Concurrent;
using System.Net.Mqtt.Server.Protocol.V5;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer :
    IObserver<IncomingMessage3>, IObserver<IncomingMessage5>,
    IObserver<SubscribeMessage3>, IObserver<SubscribeMessage5>, IObserver<UnsubscribeMessage>,
    IObserver<PacketRxMessage>, IObserver<PacketTxMessage>
{
    //TODO: Consider using regular Dictionary<K,V> with locks to improve memory allocation during enumeration
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, Message3> retainedMessages3;
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, Message5> retainedMessages5;

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    #region Implementation of IObserver<IncomingMessage>

    void IObserver<IncomingMessage3>.OnNext(IncomingMessage3 incomingMessage)
    {
        var (sender, message) = incomingMessage;
        var (topic, payload, qos, retain) = message;

        if (retain)
        {
            if (payload.Length == 0)
            {
                retainedMessages3.TryRemove(topic, out _);
            }
            else
            {
                retainedMessages3.AddOrUpdate(topic,
                    static (_, state) => state,
                    static (_, _, state) => state,
                    message);
            }
        }

        hub3?.DispatchMessage(message);
        hub4?.DispatchMessage(message);
        hub5?.DispatchMessage(new(message.Topic, message.Payload, message.QoSLevel, message.Retain));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogIncomingMessage(sender.ClientId, UTF8.GetString(topic.Span), payload.Length, qos, retain);
        }
    }

    #endregion

    #region Implementation of IObserver<IncomingMessage5>

    void IObserver<IncomingMessage5>.OnNext(IncomingMessage5 incomingMessage)
    {
        var (sender, message5) = incomingMessage;
        var (topic, payload, qos, retain) = message5;
        var message3 = new Message3(topic, payload, qos, retain);

        if (retain)
        {
            if (payload.Length == 0)
            {
                retainedMessages5.TryRemove(topic, out _);
            }
            else
            {
                retainedMessages5.AddOrUpdate(topic,
                    static (_, state) => state,
                    static (_, _, state) => state,
                    message5);
            }
        }

        hub5?.DispatchMessage(message5);
        hub3?.DispatchMessage(message3);
        hub4?.DispatchMessage(message3);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogIncomingMessage(sender.ClientId, UTF8.GetString(topic.Span), payload.Length, qos, retain);
        }
    }

    #endregion

    #region Implementation of IObserver<SubscribeMessage3>

    void IObserver<SubscribeMessage3>.OnNext(SubscribeMessage3 request)
    {
        var writer = request.Sender.OutgoingWriter;
        foreach ((ReadOnlySpan<byte> filter, var qos) in request.Filters)
        {
            foreach (var (topic, message) in retainedMessages3)
            {
                if (MqttExtensions.TopicMatches(topic.Span, filter))
                    writer.TryWrite(message with { QoSLevel = Math.Min(qos, message.QoSLevel) });
            }

            foreach (var (topic, message) in retainedMessages5)
            {
                if (MqttExtensions.TopicMatches(topic.Span, filter))
                    writer.TryWrite(new(message.Topic, message.Payload, Math.Min(qos, message.QoSLevel), true));
            }
        }

        if (RuntimeSettings.MetricsCollectionSupport)
        {
            updateStatsSignal.TrySetResult();
        }
    }

    #endregion

    #region Implementation of IObserver<SubscribeMessage5>

    void IObserver<SubscribeMessage5>.OnNext(SubscribeMessage5 request)
    {
        var (sender, subscriptions) = request;
        var writer = sender.OutgoingWriter;
        var ids = subscriptions is [{ Options.SubscriptionId: not 0 and var id }, ..] ? new uint[] { id } : null;
        for (var i = 0; i < subscriptions.Count; i++)
        {
            var (filter, exists, options) = subscriptions[i];

            if (options.RetainHandling is RetainHandling.DoNotSend ||
                options.RetainHandling is RetainHandling.SendIfNew && exists)
            {
                continue;
            }

            var qos = options.QoS;
            foreach (var (topic, message) in retainedMessages5)
            {
                if (MqttExtensions.TopicMatches(topic.Span, filter))
                    writer.TryWrite(message with { QoSLevel = Math.Min(qos, message.QoSLevel), SubscriptionIds = ids });
            }

            foreach (var (topic, message) in retainedMessages3)
            {
                if (MqttExtensions.TopicMatches(topic.Span, filter))
                    writer.TryWrite(new(message.Topic, message.Payload, Math.Min(qos, message.QoSLevel), true) { SubscriptionIds = ids });
            }
        }

        if (RuntimeSettings.MetricsCollectionSupport)
        {
            updateStatsSignal.TrySetResult();
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