using KVP = (byte[] Filter, byte QoS);

namespace Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionSubscriptionState3
{
    private readonly Dictionary<byte[], byte> subscriptions = new(comparer: ByteSequenceComparer.Instance);
    private volatile KVP[] snapshot = [];

    public byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte QoS)> filters, out int currentCount)
    {
        var count = filters.Count;
        var returnCodes = new byte[count];

        for (var i = 0; i < count; i++)
        {
            var (filter, qos) = filters[i];

            var isValid = qos <= 2 && TopicHelpers.IsValidFilter(filter);
            if (isValid)
            {
                subscriptions[filter] = qos;
            }

            returnCodes[i] = GetReturnCode(isValid, qos);
        }

        snapshot = GetReaderOptimizedSnapshot(subscriptions);
        currentCount = subscriptions.Count;
        return returnCodes;
    }

    public void Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        for (var i = 0; i < filters.Count; i++)
        {
            subscriptions.Remove(filters[i]);
        }

        currentCount = subscriptions.Count;
    }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out QoSLevel maxQoS)
    {
        var maxLevel = -1;

        var current = snapshot;

        for (var i = 0; i < current.Length; i++)
        {
            var level = current[i].QoS;
            if (level > maxLevel && TopicHelpers.TopicMatches(topic, current[i].Filter))
            {
                maxLevel = level;
            }
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

    private static KVP[] GetReaderOptimizedSnapshot(Dictionary<byte[], byte> subscriptions)
    {
        var index = 0;
        var data = new KVP[subscriptions.Count];

        foreach (var kvp in subscriptions)
        {
            data[index++] = (kvp.Key, kvp.Value);
        }

        return data;
    }

    protected virtual byte GetReturnCode(bool valid, byte qos) => qos;
}