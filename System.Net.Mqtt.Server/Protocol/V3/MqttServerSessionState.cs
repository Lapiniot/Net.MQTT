using System.Diagnostics.CodeAnalysis;
using System.Memory;
using System.Net.Mqtt.Extensions;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly int parallelThreshold;
    private readonly Dictionary<string, byte> subscriptions;

    public MqttServerSessionState(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, Channel.CreateUnbounded<Message>(), createdAt, maxInFlight)
    {
        subscriptions = new();
        lockSlim = new(LockRecursionPolicy.NoRecursion);
        parallelThreshold = 8;
    }

    public Message? WillMessage { get; set; }

    public override void Trim()
    {
        subscriptions.TrimExcess();
        base.Trim();
    }

    #region Subscription management

    public override bool TopicMatches(string topic, out byte maxQoS)
    {
        try
        {
            lockSlim.EnterReadLock();

            int maxLevel;
            try
            {
                maxLevel = subscriptions.Count <= parallelThreshold
                    ? SequentialMatch(topic)
                    : ParallelMatch(topic);
            }
            finally
            {
                lockSlim.ExitReadLock();
            }

            maxQoS = (byte)maxLevel;
            return maxLevel >= 0;
        }
        catch (ObjectDisposedException)
        {
            maxQoS = 0;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SequentialMatch(string topic)
    {
        var maxLevel = -1;
        var topicSpan = topic.AsSpan();

        foreach (var (filter, level) in subscriptions)
        {
            if (MqttExtensions.TopicMatches(topicSpan, filter) && level > maxLevel)
            {
                maxLevel = level;
            }
        }

        return maxLevel;
    }

    private int ParallelMatch(string topic)
    {
        var state = ObjectPool<ParallelTopicMatchState>.Shared.Rent();

        try
        {
            state.Topic = topic;
            state.MaxQoS = -1;

            Parallel.ForEach(subscriptions, static () => -1, state.Match, state.Aggregate);
            return state.MaxQoS;
        }
        finally
        {
            ObjectPool<ParallelTopicMatchState>.Shared.Return(state);
        }
    }

    public override byte[] Subscribe([NotNull] IReadOnlyList<(string Filter, byte QoS)> filters)
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
                lockSlim.ExitWriteLock();
            }

            return feedback;
        }
        catch (ObjectDisposedException)
        {
            return Array.Empty<byte>();
        }
    }

    protected virtual byte AddFilter(string filter, byte qosLevel)
    {
        TryAdd(filter, qosLevel);
        return qosLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected bool TryAdd(string filter, byte qosLevel)
    {
        if (!MqttExtensions.IsValidFilter(filter) || qosLevel > 2) return false;
        subscriptions[filter] = qosLevel;
        return true;
    }

    public override void Unsubscribe([NotNull] IReadOnlyList<string> filters)
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
                lockSlim.ExitWriteLock();
            }
        }
        catch (ObjectDisposedException)
        { }
    }

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