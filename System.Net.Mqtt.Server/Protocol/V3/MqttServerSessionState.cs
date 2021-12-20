using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt.Extensions;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Server.Protocol.V3;

internal readonly record struct MessageBlock(ushort Id, byte Flags, string Topic, in ReadOnlyMemory<byte> Payload);
public delegate void PubRelDispatchHandler(ushort id);
public delegate void PublishDispatchHandler(ushort id, byte flags, string topic, in ReadOnlyMemory<byte> payload);
public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly Dictionary<string, byte> subscriptions;
    private bool disposed;
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly int parralelThreshold;
    private readonly IdentityPool idPool;
    private readonly ChannelReader<Message> reader;
    private readonly ChannelWriter<Message> writer;
    private readonly HashSet<ushort> receivedQos2;
    private readonly HashQueueCollection<ushort, MessageBlock> resendQueue;

    public MqttServerSessionState(string clientId, DateTime createdAt) : base(clientId, createdAt)
    {
        subscriptions = new Dictionary<string, byte>();
        idPool = new FastIdentityPool();
        receivedQos2 = new HashSet<ushort>();
        resendQueue = new HashQueueCollection<ushort, MessageBlock>();
        lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        parralelThreshold = 8;
        (reader, writer) = Channel.CreateUnbounded<Message>();
    }

    public bool IsActive { get; set; }
    public Message? WillMessage { get; set; }

    public bool TryAddQoS2(ushort packetId)
    {
        return receivedQos2.Add(packetId);
    }

    public bool RemoveQoS2(ushort packetId)
    {
        return receivedQos2.Remove(packetId);
    }

    public ushort AddPublishToResend(string topic, ReadOnlyMemory<byte> payload, byte qoSLevel)
    {
        var id = idPool.Rent();
        var message = new MessageBlock(id, (byte)(Duplicate | (qoSLevel << 1)), topic, payload);
        resendQueue.AddOrUpdate(id, message, message);
        return id;
    }

    public void AddPubRelToResend(ushort id)
    {
        var message = new MessageBlock() { Id = id };
        resendQueue.AddOrUpdate(id, message, message);
    }

    public bool RemoveFromResend(ushort id)
    {
        if(!resendQueue.TryRemove(id, out _)) return false;
        idPool.Release(id);
        return true;
    }

    public void DispatchPendingMessages([NotNull] PubRelDispatchHandler pubRelHandler, [NotNull] PublishDispatchHandler publishHandler)
    {
        foreach(var (id, flags, topic, payload) in resendQueue)
        {
            if(topic is null)
            {
                pubRelHandler(id);
            }
            else
            {
                publishHandler(id, flags, topic, payload);
            }
        }
    }

    #region Incoming message delivery state

    public override ValueTask EnqueueAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Skip all incoming QoS 0 if session is inactive
        return !IsActive && message.QoSLevel == 0 ? ValueTask.CompletedTask : writer.WriteAsync(message, cancellationToken);
    }

    public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
    {
        return reader.ReadAsync(cancellationToken);
    }

    #endregion

    #region Implementation of IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if(disposed) return;

        if(disposing)
        {
            using(resendQueue)
            using(lockSlim) { }
        }

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Subscription management

    public override bool TopicMatches(string topic, out byte qos)
    {
        int maxQoS = -1;

        lockSlim.EnterReadLock();

        try
        {
            if(subscriptions.Count <= parralelThreshold)
            {
                foreach(var (filter, level) in subscriptions)
                {
                    if(MqttExtensions.TopicMatches(topic, filter) && level > maxQoS)
                    {
                        maxQoS = level;
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
                    var current = Volatile.Read(ref maxQoS);
                    for(int i = current; i < level; i++)
                    {
                        int value = Interlocked.CompareExchange(ref maxQoS, level, i);
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

        qos = (byte)maxQoS;
        return maxQoS >= 0;
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