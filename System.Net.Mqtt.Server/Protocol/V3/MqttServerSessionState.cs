using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly int parallelThreshold;
    private readonly Dictionary<string, (Utf8String TopicBytes, byte QoS)> subscriptions;

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

    public override bool TopicMatches(Utf8String topic, out byte maxQoS)
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
    private int SequentialMatch(Utf8String topic)
    {
        var maxLevel = -1;

        var topicSpan = topic.Span;

        foreach (var (_, (filter, level)) in subscriptions)
        {
            if (MqttExtensions.TopicMatches(topicSpan, filter.Span) && level > maxLevel)
            {
                maxLevel = level;
            }
        }

        return maxLevel;
    }

    private int ParallelMatch(Utf8String topic)
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

    public override byte[] Subscribe([NotNull] IReadOnlyList<(Utf8String Filter, byte QoS)> filters)
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

    protected virtual byte AddFilter(Utf8String filter, byte qosLevel)
    {
        TryAdd(filter, qosLevel);
        return qosLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected bool TryAdd(Utf8String filter, byte qosLevel)
    {
        var span = filter.Span;
        if (!MqttExtensions.IsValidFilter(span) || qosLevel > 2) return false;
        subscriptions[UTF8.GetString(span)] = (filter, qosLevel);
        return true;
    }

    public override void Unsubscribe([NotNull] IReadOnlyList<Utf8String> filters)
    {
        try
        {
            lockSlim.EnterWriteLock();

            try
            {
                for (var i = 0; i < filters.Count; i++)
                {
                    subscriptions.Remove(UTF8.GetString(filters[i].Span));
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