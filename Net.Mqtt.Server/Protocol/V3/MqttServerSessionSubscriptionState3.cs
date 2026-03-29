namespace Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionSubscriptionState3
{
    private volatile Dictionary<byte[], byte> subscriptions = [];

    public int SubscriptionsCount => subscriptions.Count;

    public bool TopicMatches(ReadOnlySpan<byte> topic, out QoSLevel maxQoS)
    {
        var maxLevel = -1;

        foreach (var (filter, level) in subscriptions)
        {
            if (TopicHelpers.TopicMatches(topic, filter) && level > maxLevel)
                maxLevel = level;
        }

        if (maxLevel >= 0)
        {
            maxQoS = (QoSLevel)maxLevel;
            return true;
        }
        else
        {
            maxQoS = default;
            return false;
        }
    }

    public byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte QoS)> filters, out int currentCount)
    {
        var count = filters.Count;
        var returnCodes = new byte[count];
        var cloned = new Dictionary<byte[], byte>(subscriptions, ByteSequenceComparer.Instance);

        for (var i = 0; i < count; i++)
        {
            var (filter, qos) = filters[i];

            var isValid = qos <= 2 && TopicHelpers.IsValidFilter(filter);
            if (isValid)
            {
                cloned[filter] = qos;
            }

            returnCodes[i] = GetReturnCode(isValid, qos);
        }

        subscriptions = cloned;
        currentCount = cloned.Count;
        return returnCodes;
    }

    public void Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var cloned = new Dictionary<byte[], byte>(subscriptions, ByteSequenceComparer.Instance);
        for (var i = 0; i < filters.Count; i++)
        {
            cloned.Remove(filters[i]);
        }

        subscriptions = cloned;
        currentCount = cloned.Count;
    }

    protected virtual byte GetReturnCode(bool valid, byte qos) => qos;
}