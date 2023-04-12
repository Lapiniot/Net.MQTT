
namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState3 : MqttServerSessionState
{
    private readonly Dictionary<byte[], byte> subscriptions;
    private SpinLock spinLock; // do not mark field readonly because struct is mutable!!!

    public MqttServerSessionState3(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, Channel.CreateUnbounded<Message>(), createdAt, maxInFlight)
    {
        subscriptions = new(ByteSequenceComparer.Instance);
        spinLock = new(false);
    }

    public Message? WillMessage { get; set; }

    public override void Trim()
    {
        subscriptions.TrimExcess();
        base.Trim();
    }

    #region Subscription management

    public override bool TopicMatches(ReadOnlySpan<byte> topic, out byte maxQoS)
    {
        var taken = false;

        try
        {
            spinLock.Enter(ref taken);
            var maxLevel = -1;

            foreach (var (filter, level) in subscriptions)
            {
                if (MqttExtensions.TopicMatches(topic, filter) && level > maxLevel)
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

    public sealed override byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte QoS)> filters, out int currentCount)
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

    protected virtual byte AddFilter(byte[] filter, byte qosLevel)
    {
        TryAdd(filter, qosLevel);
        return qosLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryAdd(byte[] filter, byte qosLevel)
    {
        if (!MqttExtensions.IsValidFilter(filter) || qosLevel > 2) return false;
        subscriptions[filter] = qosLevel;
        return true;
    }

    public sealed override void Unsubscribe([NotNull] IReadOnlyList<byte[]> filters, out int currentCount)
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

    public sealed override int GetSubscriptionsCount() => subscriptions.Count;

    #endregion
}