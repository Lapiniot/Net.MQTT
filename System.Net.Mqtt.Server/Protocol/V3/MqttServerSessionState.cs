using System.Diagnostics.CodeAnalysis;
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

    public override ValueTask EnqueueAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return writer.WriteAsync(message, cancellationToken);
    }

    public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
    {
        return reader.ReadAsync(cancellationToken);
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

    #region Subscription management

    public override bool TopicMatches(string topic, out byte maxQoS)
    {
        int maxLevel = -1;

        lockSlim.EnterReadLock();

        try
        {
            if(subscriptions.Count <= parralelThreshold)
            {
                foreach(var (filter, level) in subscriptions)
                {
                    if(MqttExtensions.TopicMatches(topic, filter) && level > maxLevel)
                    {
                        maxLevel = level;
                    }
                }
            }
            else
            {
                int Init()
                {
                    return -1;
                }

                int Match(KeyValuePair<string, byte> pair, ParallelLoopState state, int qos)
                {
                    var (filter, level) = pair;
                    return MqttExtensions.TopicMatches(topic, filter) && level > qos ? level : qos;
                }

                void Aggregate(int level)
                {
                    var current = Volatile.Read(ref maxLevel);
                    for(int i = current; i < level; i++)
                    {
                        int value = Interlocked.CompareExchange(ref maxLevel, level, i);
                        if(value == i || value >= level)
                        {
                            break;
                        }
                    }
                }

                Parallel.ForEach(subscriptions, Init, Match, Aggregate);
            }
        }
        finally
        {
            lockSlim.ExitReadLock();
        }

        maxQoS = (byte)maxLevel;
        return maxLevel >= 0;
    }

    public override byte[] Subscribe([NotNull] (string Filter, byte QosLevel)[] filters)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    #endregion
}