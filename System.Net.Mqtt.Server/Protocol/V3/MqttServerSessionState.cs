using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly Dictionary<string, byte> subscriptions;
    private bool disposed;
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly int parralelThreshold;

    public MqttServerSessionState(string clientId, DateTime createdAt) : base(clientId, createdAt)
    {
        subscriptions = new Dictionary<string, byte>();
        IdPool = new FastIdentityPool();
        ReceivedQos2 = new HashSet<ushort>();
        ResendQueue = new HashQueueCollection<ushort, MqttPacket>();
        lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        parralelThreshold = 8;

        (Reader, Writer) = Channel.CreateUnbounded<Message>();
    }

    protected IdentityPool IdPool { get; }
    protected ChannelReader<Message> Reader { get; }
    protected HashSet<ushort> ReceivedQos2 { get; }
    protected HashQueueCollection<ushort, MqttPacket> ResendQueue { get; }
    protected ChannelWriter<Message> Writer { get; }

    public bool IsActive { get; set; }

    public Message? WillMessage { get; set; }

    public bool TryAddQoS2(ushort packetId)
    {
        return ReceivedQos2.Add(packetId);
    }

    public bool RemoveQoS2(ushort packetId)
    {
        return ReceivedQos2.Remove(packetId);
    }

    public ushort AddPublishToResend(string topic, ReadOnlyMemory<byte> payload, byte qoSLevel)
    {
        var id = IdPool.Rent();
        var packet = new PublishPacket(id, qoSLevel, topic, payload, duplicate: true);
        ResendQueue.AddOrUpdate(id, packet, packet);
        return id;
    }

    public PubRelPacket AddPubRelToResend(ushort id)
    {
        var pubRelPacket = new PubRelPacket(id);
        ResendQueue.AddOrUpdate(id, pubRelPacket, pubRelPacket);
        return pubRelPacket;
    }

    public bool RemoveFromResend(ushort id)
    {
        if(!ResendQueue.TryRemove(id, out _)) return false;
        IdPool.Release(id);
        return true;
    }

    public IEnumerable<MqttPacket> ResendPackets => ResendQueue;

    #region Incoming message delivery state

    public override ValueTask EnqueueAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Skip all incoming QoS 0 if session is inactive
        return !IsActive && message.QoSLevel == 0 ? ValueTask.CompletedTask : Writer.WriteAsync(message, cancellationToken);
    }

    public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
    {
        return Reader.ReadAsync(cancellationToken);
    }

    #endregion

    #region Implementation of IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if(disposed) return;

        if(disposing)
        {
            ResendQueue.Dispose();
            lockSlim.Dispose();
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
                feedback[qosLevel] = AddFilter(filter, qosLevel);
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