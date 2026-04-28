using System.Runtime.InteropServices;

#nullable enable

namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

public sealed class MqttServerSessionSubscriptionState5V5
{
    private sealed record class Snapshot(byte[][] Keys, SubscriptionOptions[] Values);
    private readonly Dictionary<byte[], SubscriptionOptions> subscriptions = new(comparer: ByteSequenceComparer.Instance);
    private volatile Snapshot snapshot = new([], []);

    public SubscribeResult Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte Flags)> filters, uint subscriptionId)
    {
        var count = filters.Count;
        var returnCodes = new byte[count];
        var subs = new List<(byte[] Filter, bool Exists, SubscriptionOptions Options)>(count);

        for (var i = 0; i < count; i++)
        {
            var (filter, options) = filters[i];
            var qos = (byte)(options & PacketFlags.QoSMask);
            if (qos <= 2 && TopicHelpers.IsValidFilter(filter))
            {
                returnCodes[i] = qos;
                ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(subscriptions, filter, out var exists);
                valueRef = new(qos, options, subscriptionId);
                subs.Add((filter, exists, valueRef));
            }
            else
            {
                returnCodes[i] = 0x80;
            }
        }

        snapshot = GetReaderOptimizedSnapshot(subscriptions);
        return new(ImmutableCollectionsMarshal.AsImmutableArray(returnCodes),
            Subscriptions: subs.AsReadOnly(),
            TotalCount: subscriptions.Count);
    }

    public byte[] Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var count = filters.Count;
        var returnCodes = new byte[count];

        for (var i = 0; i < count; i++)
        {
            returnCodes[i] = subscriptions.Remove(filters[i]) ? (byte)0x00 : (byte)0x11;
        }

        snapshot = GetReaderOptimizedSnapshot(subscriptions);
        currentCount = subscriptions.Count;
        return returnCodes;
    }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions options, out IReadOnlyList<uint>? subscriptionIds)
    {
        options = default;
        List<uint>? ids = null;
        var maxQoS = -1;

        var current = snapshot;
        var keys = current.Keys.AsSpan();
        var values = current.Values.AsSpan();

        for (var i = 0; i < keys.Length; i++)
        {
            if (TopicHelpers.TopicMatches(topic, filter: keys[i]))
            {
                ref var opts = ref values[i];

                var id = opts.SubscriptionId;
                if (id is not 0)
                {
                    (ids ??= []).Add(id);
                }

                var qos = opts.QoS;
                if (qos > maxQoS)
                {
                    maxQoS = qos;
                    options = opts;
                }
            }
        }

        subscriptionIds = ids?.AsReadOnly();
        return maxQoS >= 0;
    }

    [MethodImpl(AggressiveInlining)]
    private static Snapshot GetReaderOptimizedSnapshot(Dictionary<byte[], SubscriptionOptions> subscriptions)
    {
        var index = 0;
        var count = subscriptions.Count;
        var keys = new byte[count][];
        var values = new SubscriptionOptions[count];

        foreach (var (key, value) in subscriptions)
        {
            keys[index] = key;
            values[index++] = value;
        }

        return new(keys, values);
    }
}