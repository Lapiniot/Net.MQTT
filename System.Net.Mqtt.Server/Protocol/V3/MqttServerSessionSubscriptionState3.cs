
namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionSubscriptionState3
{
    private readonly Dictionary<byte[], byte> subscriptions;
    private SpinLock spinLock; // do not mark field readonly because struct is mutable!!!

    public MqttServerSessionSubscriptionState3()
    {
        subscriptions = new(ByteSequenceComparer.Instance);
        spinLock = new(false);
    }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out byte maxQoS)
    {
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            var maxLevel = -1;

            foreach (var (filter, level) in subscriptions)
            {
                if (TopicHelpers.TopicMatches(topic, filter) && level > maxLevel)
                {
                    maxLevel = level;
                }
            }

            if (maxLevel >= 0)
            {
                maxQoS = (byte)maxLevel;
                return true;
            }
            else
            {
                maxQoS = 0;
                return false;
            }
        }
        finally
        {
            if (taken)
                spinLock.Exit(false);
        }
    }

    public byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte Options)> filters, out int currentCount)
    {
        var feedback = new byte[filters.Count];
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            for (var i = 0; i < filters.Count; i++)
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

    protected virtual byte AddFilter(byte[] filter, byte options)
    {
        TryAdd(filter, options);
        return options;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryAdd(byte[] filter, byte qosLevel)
    {
        if (!TopicHelpers.IsValidFilter(filter) || qosLevel > 2) return false;
        subscriptions[filter] = qosLevel;
        return true;
    }

    public void Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            for (var i = 0; i < filters.Count; i++)
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