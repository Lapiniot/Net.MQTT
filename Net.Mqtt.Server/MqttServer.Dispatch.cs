namespace Net.Mqtt.Server;

public sealed partial class MqttServer :
    IObserver<IncomingMessage3>, IObserver<IncomingMessage5>,
    IObserver<SubscribeMessage3>, IObserver<SubscribeMessage5>, IObserver<UnsubscribeMessage>,
    IObserver<PacketRxMessage>, IObserver<PacketTxMessage>
{
    private readonly RetainedMessageHandler3 retained3;
    private readonly RetainedMessageHandler5 retained5;

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    #region Implementation of IObserver<IncomingMessage>

    void IObserver<IncomingMessage3>.OnNext(IncomingMessage3 incomingMessage)
    {
        var (sender, message) = incomingMessage;
        var (topic, payload, qos, retain) = message;

        if (retain)
        {
            retained3.Update(message);
        }

        hub3?.DispatchMessage(sender, message);
        hub4?.DispatchMessage(sender, message);
        hub5?.DispatchMessage(sender, new(message.Topic, message.Payload, message.QoSLevel, message.Retain));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogIncomingMessage(sender.ClientId, UTF8.GetString(topic.Span), payload.Length, (int)qos, retain);
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
            retained5.Update(message5);
        }

        hub5?.DispatchMessage(sender, message5);
        hub3?.DispatchMessage(sender, message3);
        hub4?.DispatchMessage(sender, message3);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogIncomingMessage(sender.ClientId, UTF8.GetString(topic.Span), payload.Length, (int)qos, retain);
        }
    }

    #endregion

    #region Implementation of IObserver<SubscribeMessage3>

    void IObserver<SubscribeMessage3>.OnNext(SubscribeMessage3 request)
    {
        var (sender, subscriptions) = request;
        var count = subscriptions.Count;
        for (var i = 0; i < count; i++)
        {
            var subscription = subscriptions[i];
            retained3.OnNext(sender, subscription);
            retained5.OnNext(sender, subscription);
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
        var count = subscriptions.Count;
        for (var i = 0; i < count; i++)
        {
            var subscription = subscriptions[i];
            retained5.OnNext(sender, subscription);
            retained3.OnNext(sender, subscription);
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