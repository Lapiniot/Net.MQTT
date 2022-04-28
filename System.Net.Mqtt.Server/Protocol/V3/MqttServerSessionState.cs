using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt.Extensions;
using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly Dictionary<Utf8String, byte> subscriptions;

    public MqttServerSessionState(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, Channel.CreateUnbounded<Message>(), createdAt, maxInFlight)
    {
        subscriptions = new(Utf8StringComparer.Instance);
        lockSlim = new(LockRecursionPolicy.NoRecursion);
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
        maxQoS = 0;

        try
        {
            lockSlim.EnterReadLock();

            try
            {
                var maxLevel = -1;

                foreach (var (filter, level) in subscriptions)
                {
                    if (MqttExtensions.TopicMatches(topic.Span, filter.Span) && level > maxLevel)
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
        subscriptions[filter] = qosLevel;
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