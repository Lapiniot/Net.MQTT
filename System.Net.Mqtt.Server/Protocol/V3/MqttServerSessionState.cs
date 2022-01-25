using System.Diagnostics.CodeAnalysis;
using System.Memory;
using System.Net.Mqtt.Extensions;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState
{
    private readonly Dictionary<string, byte> subscriptions;
    private bool disposed;
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly int parralelThreshold;
    private readonly ChannelReader<Message> reader;
    private readonly ChannelWriter<Message> writer;

    public MqttServerSessionState(string clientId, DateTime createdAt) : base(clientId, createdAt)
    {
        subscriptions = new Dictionary<string, byte>();
        lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        parralelThreshold = 8;
        (reader, writer) = Channel.CreateUnbounded<Message>();
    }

    public Message? WillMessage { get; set; }

    #region Incoming message delivery state

    public override bool TryEnqueue(Message message)
    {
        return writer.TryWrite(message);
    }

    public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
    {
        return reader.ReadAsync(cancellationToken);
    }

    #endregion

    #region Subscription management

    public override bool TopicMatches(string topic, out byte maxQoS)
    {
        int maxLevel = -1;

        try
        {
            lockSlim.EnterReadLock();

            try
            {
                maxLevel = subscriptions.Count <= parralelThreshold
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
        catch(ObjectDisposedException)
        {
            maxQoS = 0;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SequentialMatch(string topic)
    {
        int maxLevel = -1;

        foreach(var (filter, level) in subscriptions)
        {
            if(MqttExtensions.TopicMatches(topic, filter) && level > maxLevel)
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

    public override byte[] Subscribe([NotNull] (string Filter, byte QosLevel)[] filters)
    {
        try
        {
            var feedback = new byte[filters.Length];

            lockSlim.EnterWriteLock();

            try
            {
                for(int i = 0; i < filters.Length; i++)
                {
                    var (filter, qosLevel) = filters[i];
                    feedback[i] = AddFilter(filter, qosLevel);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }

            return feedback;
        }
        catch(ObjectDisposedException)
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
        if(!MqttExtensions.IsValidFilter(filter)) return false;
        subscriptions[filter] = qosLevel;
        return true;
    }

    public override void Unsubscribe([NotNull] string[] filters)
    {
        try
        {
            lockSlim.EnterWriteLock();

            try
            {
                for(int i = 0; i < filters.Length; i++)
                {
                    subscriptions.Remove(filters[i]);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
        catch(ObjectDisposedException)
        {
        }
    }

    #endregion

    #region Implementation of IDisposable

    protected override void Dispose(bool disposing)
    {
        if(disposed) return;

        if(disposing)
        {
            using(lockSlim) { }
        }

        base.Dispose(disposing);

        disposed = true;
    }

    #endregion
}