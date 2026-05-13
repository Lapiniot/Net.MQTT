using System.Collections.Immutable;
using System.Runtime.InteropServices;
using KVP = (byte[] Filter, Net.Mqtt.Server.Protocol.V5.SubscriptionOptions Options);

namespace Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionSubscriptionState5
{
    private readonly Dictionary<byte[], SubscriptionOptions> subscriptions = [with(ByteSequenceComparer.Instance)];
    private volatile KVP[] snapshot = [];

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
        return new(ReturnCodes: ImmutableCollectionsMarshal.AsImmutableArray(returnCodes),
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
        for (var i = 0; i < current.Length; i++)
        {
            if (TopicHelpers.TopicMatches(topic, current[i].Filter))
            {
                ref var optionsRef = ref current[i].Options;
                var id = optionsRef.SubscriptionId;
                if (id is not 0)
                {
                    (ids ??= []).Add(id);
                }

                var qos = optionsRef.QoS;
                if (qos > maxQoS)
                {
                    maxQoS = qos;
                    options = optionsRef;
                }
            }
        }

        subscriptionIds = ids?.AsReadOnly();
        return maxQoS >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static KVP[] GetReaderOptimizedSnapshot(Dictionary<byte[], SubscriptionOptions> subscriptions)
    {
        var index = 0;
        var data = new KVP[subscriptions.Count];

        foreach (var kvp in subscriptions)
        {
            data[index++] = (kvp.Key, kvp.Value);
        }

        return data;
    }
}

public record SubscribeResult(
    ImmutableArray<byte> ReturnCodes,
    IReadOnlyList<(byte[] Filter, bool Exists, SubscriptionOptions Options)> Subscriptions,
    int TotalCount);