using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Net.Mqtt.Server.Protocol.V5;

#nullable enable

namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

public sealed class MqttServerSessionSubscriptionState5V1
{
    private readonly Dictionary<byte[], SubscriptionOptions> subscriptions;
    private SpinLock spinLock; // do not mark field readonly because struct is mutable!!!

    public MqttServerSessionSubscriptionState5V1()
    {
        subscriptions = new(ByteSequenceComparer.Instance);
        spinLock = new(false);
    }

    public SubscribeResult Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte Flags)> filters, uint? subscriptionId)
    {
        var count = filters.Count;
        var feedback = new byte[count];
        var subs = new List<(byte[] Filter, bool Exists, SubscriptionOptions Options)>(count);
        var total = 0;
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            var subsId = subscriptionId.GetValueOrDefault();
            for (var i = 0; i < count; i++)
            {
                var (filter, options) = filters[i];
                var qos = (byte)(options & PacketFlags.QoSMask);
                if (TopicHelpers.IsValidFilter(filter) && qos <= 2)
                {
                    feedback[i] = qos;
                    ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(subscriptions, filter, out var exists);
                    valueRef = new(qos, options, subsId);
                    subs.Add((filter, exists, valueRef));
                }
                else
                {
                    feedback[i] = 0x80;
                }
            }

            total = subscriptions.Count;
        }
        finally
        {
            if (taken)
                spinLock.Exit(false);
        }

        return new SubscribeResult(ImmutableCollectionsMarshal.AsImmutableArray(feedback), subs.AsReadOnly(), total);
    }

    public byte[] Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
    {
        var count = filters.Count;
        var feedback = new byte[count];
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            for (var i = 0; i < count; i++)
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

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions options, out IReadOnlyList<uint>? subscriptionIds)
    {
        options = default;
        var taken = false;
        List<uint>? ids = null;

        try
        {
            spinLock.Enter(ref taken);
            var max = -1;

            foreach (var (filter, opts) in subscriptions)
            {
                if (TopicHelpers.TopicMatches(topic, filter))
                {
                    if (opts.SubscriptionId is not 0)
                        (ids ??= []).Add(opts.SubscriptionId);

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

public record SubscribeResult(ImmutableArray<byte> Feedback, IReadOnlyList<(byte[] Filter, bool Exists, SubscriptionOptions Options)> Subscriptions, int TotalCount);