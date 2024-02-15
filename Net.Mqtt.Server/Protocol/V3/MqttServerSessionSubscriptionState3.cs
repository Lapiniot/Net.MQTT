namespace Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionSubscriptionState3
{
    private readonly Dictionary<byte[], byte> subscriptions;
    private SpinLock spinLock; // do not mark field readonly because struct is mutable!!!

    public MqttServerSessionSubscriptionState3()
    {
        subscriptions = new(ByteSequenceComparer.Instance);
        spinLock = new(false);
    }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out QoSLevel maxQoS)
    {
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
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
        finally
        {
            if (taken)
                spinLock.Exit(false);
        }
    }

    public byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte QoS)> filters, out int currentCount)
    {
        var feedback = new byte[filters.Count];
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            var count = filters.Count;
            for (var i = 0; i < count; i++)
            {
                var (filter, qos) = filters[i];
                feedback[i] = AddFilter(filter, qos);
            }

            currentCount = subscriptions.Count;
        }
        finally
        {
            if (taken)
                spinLock.Exit(false);
        }

        return feedback;
    }

    protected virtual byte AddFilter(byte[] filter, byte qos)
    {
        TryAdd(filter, qos);
        return qos;
    }

    protected bool TryAdd(byte[] filter, byte qos)
    {
        if (!TopicHelpers.IsValidFilter(filter) || qos > 2) return false;
        subscriptions[filter] = qos;
        return true;
    }

    public void Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            var count = filters.Count;
            for (var i = 0; i < count; i++)
            {
                subscriptions.Remove(filters[i]);
            }

            currentCount = subscriptions.Count;
        }
        finally
        {
            if (taken)
                spinLock.Exit(false);
        }
    }

    public int SubscriptionsCount => subscriptions.Count;

    public void Trim() => subscriptions.TrimExcess();
}