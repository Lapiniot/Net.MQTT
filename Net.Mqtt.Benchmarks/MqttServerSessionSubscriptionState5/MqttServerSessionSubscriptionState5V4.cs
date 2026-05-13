using System.Collections.Frozen;
using System.Runtime.InteropServices;

#nullable enable

namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

public sealed class MqttServerSessionSubscriptionState5V4
{
    private readonly Dictionary<byte[], SubscriptionOptions> subscriptions = [with(ByteSequenceComparer.Instance)];
    private volatile FrozenDictionary<byte[], SubscriptionOptions> frozen = FrozenDictionary<byte[], SubscriptionOptions>.Empty;

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

        frozen = subscriptions.ToFrozenDictionary(comparer: ByteSequenceComparer.Instance);
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

        frozen = subscriptions.ToFrozenDictionary(comparer: ByteSequenceComparer.Instance);
        currentCount = subscriptions.Count;
        return returnCodes;
    }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions options, out IReadOnlyList<uint>? subscriptionIds)
    {
        options = default;
        List<uint>? ids = null;
        var maxQoS = -1;

        var snapshot = frozen;
        var keys = snapshot.Keys.AsSpan();
        var values = snapshot.Values.AsSpan();

        for (var i = 0; i < keys.Length; i++)
        {
            if (TopicHelpers.TopicMatches(topic, filter: keys[i]))
            {
                ref readonly var opts = ref values[i];

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
}