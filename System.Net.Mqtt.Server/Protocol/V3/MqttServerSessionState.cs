namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly Dictionary<byte[], byte> subscriptions;

    public MqttServerSessionState(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, Channel.CreateUnbounded<Message>(), createdAt, maxInFlight)
    {
        subscriptions = new(ByteSequenceComparer.Instance);
        lockSlim = new(LockRecursionPolicy.NoRecursion);
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
        maxQoS = 0;

        try
        {
            lockSlim.EnterReadLock();

            try
            {
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
                    return false;
                }
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public sealed override byte[] Subscribe([NotNull] IReadOnlyList<(byte[] Filter, byte QoS)> filters, out int currentCount)
    {
        try
        {
            var feedback = new byte[filters.Count];

            lockSlim.EnterWriteLock();

            try
            {
                for (var i = 0; i < filters.Count; i++)
                {
                    var (filter, qos) = filters[i];
                    feedback[i] = AddFilter(filter, qos);
                }
            }
            finally
            {
                currentCount = subscriptions.Count;
                lockSlim.ExitWriteLock();
            }

            return feedback;
        }
        catch (ObjectDisposedException)
        {
            currentCount = 0;
            return Array.Empty<byte>();
        }
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
        try
        {
            lockSlim.EnterWriteLock();

            try
            {
                for (var i = 0; i < filters.Count; i++)
                {
                    subscriptions.Remove(filters[i]);
                }
            }
            finally
            {
                currentCount = subscriptions.Count;
                lockSlim.ExitWriteLock();
            }
        }
        catch (ObjectDisposedException)
        {
            currentCount = 0;
        }
    }

    public sealed override int GetSubscriptionsCount() => subscriptions.Count;

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            lockSlim?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}