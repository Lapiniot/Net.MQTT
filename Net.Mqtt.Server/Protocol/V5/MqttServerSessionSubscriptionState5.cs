using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionSubscriptionState5
{
    private volatile Dictionary<byte[], SubscriptionOptions> subscriptions = [];

    public SubscribeResult Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte Flags)> filters, uint subscriptionId)
    {
        var count = filters.Count;
        var returnCodes = new byte[count];
        var subs = new List<(byte[] Filter, bool Exists, SubscriptionOptions Options)>(count);

        var cloned = new Dictionary<byte[], SubscriptionOptions>(
            dictionary: subscriptions,
            comparer: ByteSequenceComparer.Instance);

        for (var i = 0; i < count; i++)
        {
            var (filter, options) = filters[i];
            var qos = (byte)(options & PacketFlags.QoSMask);
            if (qos <= 2 && TopicHelpers.IsValidFilter(filter))
            {
                returnCodes[i] = qos;
                ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(cloned, filter, out var exists);
                valueRef = new(qos, options, subscriptionId);
                subs.Add((filter, exists, valueRef));
            }
            else
            {
                returnCodes[i] = 0x80;
            }
        }

        subscriptions = cloned;
        return new(ReturnCodes: ImmutableCollectionsMarshal.AsImmutableArray(returnCodes),
            Subscriptions: subs.AsReadOnly(),
            TotalCount: cloned.Count);
    }

    public byte[] Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var count = filters.Count;
        var returnCodes = new byte[count];

        var cloned = new Dictionary<byte[], SubscriptionOptions>(
            dictionary: subscriptions,
            comparer: ByteSequenceComparer.Instance);

        for (var i = 0; i < count; i++)
        {
            returnCodes[i] = cloned.Remove(filters[i]) ? (byte)0x00 : (byte)0x11;
        }

        subscriptions = cloned;
        currentCount = cloned.Count;
        return returnCodes;
    }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions options, out IReadOnlyList<uint>? subscriptionIds)
    {
        options = default;
        List<uint>? ids = null;

        var max = -1;

        foreach (var (filter, opts) in subscriptions)
        {
            if (TopicHelpers.TopicMatches(topic, filter))
            {
                if (opts.SubscriptionId is not 0)
                {
                    (ids ??= []).Add(opts.SubscriptionId);
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
}

public record SubscribeResult(
    ImmutableArray<byte> ReturnCodes,
    IReadOnlyList<(byte[] Filter, bool Exists, SubscriptionOptions Options)> Subscriptions,
    int TotalCount);