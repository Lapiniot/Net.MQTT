using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState : Server.MqttServerSessionState, IDisposable
{
    private readonly ConcurrentDictionary<string, byte> subscriptions;
    private bool disposed;

    public MqttServerSessionState(string clientId, DateTime createdAt) : base(clientId, createdAt)
    {
        subscriptions = new ConcurrentDictionary<string, byte>();
        IdPool = new FastPacketIdPool();
        ReceivedQos2 = new HashSet<ushort>();
        ResendQueue = new HashQueueCollection<ushort, MqttPacket>();

        (Reader, Writer) = Channel.CreateUnbounded<Message>();
    }

    protected IPacketIdPool IdPool { get; }
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

    public PublishPacket AddPublishToResend(string topic, Memory<byte> payload, byte qoSLevel)
    {
        var id = IdPool.Rent();
        var packet = new PublishPacket(id, qoSLevel, topic, payload, duplicate: true);
        ResendQueue.AddOrUpdate(id, packet, packet);
        return packet;
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


    #region Subscription state

    public override IReadOnlyDictionary<string, byte> GetSubscriptions()
    {
        return subscriptions;
    }

    protected override byte AddTopicFilter(string filter, byte qos)
    {
        return MqttExtensions.IsValidTopic(filter) ? AddOrUpdateInternal(filter, qos) : qos;
    }

    protected byte AddOrUpdateInternal(string filter, byte qos)
    {
        return subscriptions.AddOrUpdate(filter, qos, (_, __) => qos);
    }

    protected override void RemoveTopicFilter(string filter)
    {
        subscriptions.TryRemove(filter, out _);
    }

    #endregion

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
        }

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}