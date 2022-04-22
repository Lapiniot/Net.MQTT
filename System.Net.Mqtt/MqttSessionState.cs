using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt;

internal readonly record struct PacketBlock(ushort Id, byte Flags, string Topic, in ReadOnlyMemory<byte> Payload);

public delegate void PubRelDispatchHandler(ushort id);

public delegate void PublishDispatchHandler(ushort id, byte flags, string topic, in ReadOnlyMemory<byte> payload);

public abstract class MqttSessionState
{
    private readonly IdentityPool idPool;
    private readonly AsyncSemaphore inflightSentinel;
    private readonly OrderedHashMap<ushort, PacketBlock> outgoingState;
    private readonly HashSet<ushort> receivedQos2;

    protected MqttSessionState(int maxInFlight)
    {
        Verify.ThrowIfNotInRange(maxInFlight, 1, ushort.MaxValue);

        receivedQos2 = new();
        outgoingState = new(); //TODO: investigate performance with explicit capacity initially set here
        idPool = new FastIdentityPool();
        inflightSentinel = new(maxInFlight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort RentId() => idPool.Rent();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnId(ushort id) => idPool.Release(id);

    public bool TryAddQoS2(ushort packetId) => receivedQos2.Add(packetId);

    public bool RemoveQoS2(ushort packetId) => receivedQos2.Remove(packetId);

    /// <summary>
    /// Creates and stores state data to track QoS 1 or 2 application message protocol exchange
    /// </summary>
    /// <param name="flags">PUBLISH packet header flags</param>
    /// <param name="topic">PUBLISH packet topic</param>
    /// <param name="payload">PUBLISH packet payload</param>
    /// <returns>Packet Id associated with this application message protocol exchange</returns>
    public async Task<ushort> CreateMessageDeliveryStateAsync(byte flags, string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
        var id = idPool.Rent();
        var message = new PacketBlock(id, (byte)(flags | Duplicate), topic, in payload);
        outgoingState.AddOrUpdate(id, message, message);
        return id;
    }

    /// <summary>
    /// Updates QoS 2 message delivery state data to indicate PUBLISH packet has been acknowledged
    /// (in response to the corresponding PUBREC packet)
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    public void SetMessagePublishAcknowledged(ushort packetId)
    {
        var message = new PacketBlock { Id = packetId };
        outgoingState.AddOrUpdate(packetId, message, message);
    }

    /// <summary>
    /// Acknowledges application message delivery and discard all associated state data
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    public void DiscardMessageDeliveryState(ushort packetId)
    {
        if (!outgoingState.TryRemove(packetId, out _)) return;
        idPool.Release(packetId);
        inflightSentinel.Release();
    }

    public void DispatchPendingMessages([NotNull] PubRelDispatchHandler pubRelHandler, [NotNull] PublishDispatchHandler publishHandler)
    {
        // TODO: consider using Parallel.Foreach
        foreach (var (id, flags, topic, payload) in outgoingState)
        {
            if (topic is null)
            {
                pubRelHandler(id);
            }
            else
            {
                publishHandler(id, flags, topic, payload);
            }
        }
    }

    public virtual void Trim()
    {
        receivedQos2.TrimExcess();
        outgoingState.TrimExcess();
    }
}