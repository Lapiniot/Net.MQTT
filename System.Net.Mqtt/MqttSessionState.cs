namespace System.Net.Mqtt;

public readonly record struct PublishDeliveryState(byte Flags, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload);

/// <summary>
/// Base abstract type for session state
/// </summary>
/// <typeparam name="TPubState">Type of the internal QoS1 and QoS2 inflight message state</typeparam>
public abstract class MqttSessionState<TPubState>
{
    public delegate void PublishDispatchHandler(ushort id, TPubState state);

    private readonly BitSetIdentifierPool idPool;
    private readonly AsyncSemaphore inflightSentinel;
    private readonly OrderedHashMap<ushort, (ushort, TPubState)> outgoingState;
    private readonly HashSet<ushort> receivedQos2;

    protected MqttSessionState(int maxInFlight)
    {
        Verify.ThrowIfNotInRange(maxInFlight, 1, ushort.MaxValue);

        receivedQos2 = new();
        outgoingState = new(); //TODO: investigate performance with explicit capacity initially set here
        idPool = new BitSetIdentifierPool();
        inflightSentinel = new(maxInFlight);
    }

    [MethodImpl(AggressiveInlining)]
    public ushort RentId() => idPool.Rent();

    [MethodImpl(AggressiveInlining)]
    public void ReturnId(ushort id) => idPool.Return(id);

    public bool TryAddQoS2(ushort packetId) => receivedQos2.Add(packetId);

    public bool RemoveQoS2(ushort packetId) => receivedQos2.Remove(packetId);

    protected async Task<ushort> CreateMessageDeliveryStateAsync(TPubState state, CancellationToken cancellationToken)
    {
        await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
        var id = idPool.Rent();
        var s = (id, state);
        outgoingState.AddOrUpdate(id, s, s);
        return id;
    }

    /// <summary>
    /// Updates QoS 2 message delivery state data to indicate PUBLISH packet has been acknowledged
    /// (in response to the corresponding PUBREC packet)
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    public void SetMessagePublishAcknowledged(ushort packetId)
    {
        var state = (packetId, default(TPubState));
        outgoingState.AddOrUpdate(packetId, state, state);
    }

    /// <summary>
    /// Acknowledges application message delivery and discard all associated state data
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    /// <returns><value>True</value> when delivery state existed for specified
    /// <paramref name="packetId" />
    /// , otherwise <value>False</value></returns>
    protected bool DiscardMessageDeliveryState(ushort packetId)
    {
        if (!outgoingState.TryRemove(packetId, out _)) return false;
        idPool.Return(packetId);
        inflightSentinel.Release();
        return true;
    }

    public void DispatchPendingMessages([NotNull] PublishDispatchHandler publishHandler)
    {
        // TODO: consider using Parallel.Foreach
        foreach (var (id, state) in outgoingState)
        {
            publishHandler(id, state);
        }
    }

    public virtual void Trim()
    {
        receivedQos2.TrimExcess();
        outgoingState.TrimExcess();
    }
}