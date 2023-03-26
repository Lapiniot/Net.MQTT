using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IObserver<IncomingMessage>, IObserver<SubscribeMessage>, IObserver<UnsubscribeMessage>, IObserver<PacketRxMessage>, IObserver<PacketTxMessage>
{
    //TODO: Consider using regular Dictionary<K,V> with locks to improve memory allocation during enumeration
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, Message> retainedMessages;

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    #region Implementation of IObserver<IncomingMessage>

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

    #region Implementation of IObserver<SubscribeMessage>

    void IObserver<SubscribeMessage>.OnNext(SubscribeMessage request)
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