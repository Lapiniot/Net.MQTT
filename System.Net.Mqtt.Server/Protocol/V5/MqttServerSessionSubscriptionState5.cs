namespace System.Net.Mqtt.Server.Protocol.V5;

public record SubscriptionOptions(byte QoS, bool NoLocal, bool RetainAsPublished, RetainHandling RetainHandling, uint SubscriptionId);

public enum RetainHandling
{
    Send = 0,
    SendIfNew = 1,
    DoNotSend = 2
}

public sealed class MqttServerSessionSubscriptionState5
{
    private readonly Dictionary<byte[], SubscriptionOptions> subscriptions;
    private SpinLock spinLock; // do not mark field readonly because struct is mutable!!!

    public MqttServerSessionSubscriptionState5()
    {
        subscriptions = new(ByteSequenceComparer.Instance);
        spinLock = new(false);
    }

    public byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte Flags)> filters, uint? subscriptionId, out int currentCount)
    {
        var feedback = new byte[filters.Count];
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            for (var i = 0; i < filters.Count; i++)
            {
                var (filter, options) = filters[i];
                var qosLevel = (byte)(options & 0b11);
                if (MqttExtensions.IsValidFilter(filter) && qosLevel <= 2)
                {
                    feedback[i] = qosLevel;
                    subscriptions[filter] = new(qosLevel,
                        (options & 0b100) != 0, (options & 0b1000) != 0,
                        (RetainHandling)((options >>> 4) & 0b11), subscriptionId.GetValueOrDefault());
                }
                else
                {
                    feedback[i] = 0x80;
                }
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

    public byte[] Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var feedback = new byte[filters.Count];
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            for (var i = 0; i < filters.Count; i++)
            {
                feedback[i] = subscriptions.Remove(filters[i]) ? (byte)0x00 : (byte)0x11;
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

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions? options, out IReadOnlyList<uint>? subscriptionIds)
    {
        options = null;
        var taken = false;
        List<uint>? ids = null;

        try
        {
            spinLock.Enter(ref taken);
            var max = -1;

            foreach (var (filter, opts) in subscriptions)
            {
                if (MqttExtensions.TopicMatches(topic, filter))
                {
                    if (opts.SubscriptionId is not 0)
                    {
                        (ids ??= new()).Add(opts.SubscriptionId);
                    }

                    if (opts.QoS > max)
                    {
                        max = opts.QoS;
                        options = opts;
                    }
                }
            }

            subscriptionIds = ids?.AsReadOnly();
            return max >= 0;
        }
        finally
        {
            if (taken)
                spinLock.Exit(false);
        }
    }
}