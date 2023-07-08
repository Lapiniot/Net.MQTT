namespace System.Net.Mqtt;

public readonly record struct PublishDeliveryState(byte Flags, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload);

/// <summary>
/// Base abstract type for MQTT session state
/// </summary>
public abstract class MqttSessionState
{
    private volatile bool isActive;
    private string clientId;
    private int clientIdHash;

    public string ClientId
    {
        get => clientId;
        init
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            clientId = value;
            clientIdHash = value.GetHashCode(StringComparison.Ordinal);
        }
    }

    public bool IsActive { get => isActive; set => isActive = value; }

    /// <summary>
    /// Checks whether session state instances represent states for the same client 
    /// (either references are equal or have the same ClientId).
    /// </summary>
    /// <param name="state">Session state to compare.</param>
    /// <param name="other">Session state to compare with.</param>
    /// <returns><see langword="true" /> if two state instances are logically equal, otherwise <see langword="false" /></returns>
    public static bool SessionEquals([NotNull] MqttSessionState state, [NotNull] MqttSessionState other) =>
        state.clientIdHash == other.clientIdHash
            && (ReferenceEquals(state, other) ||
                string.Equals(state.clientId, other.clientId, StringComparison.Ordinal));
}

/// <summary>
/// Base abstract type for session state which provides unique 
/// packet id pool + essential message "inflight" state store implementation
/// </summary>
/// <typeparam name="TPubState">Type of the internal QoS1 and QoS2 inflight message state</typeparam>
public abstract class MqttSessionState<TPubState> : MqttSessionState
{
    private readonly BitSetIdentifierPool idPool;
    private readonly OrderedHashMap<ushort, (ushort, TPubState)> outgoingState;
    private readonly HashSet<ushort> receivedQos2;

    protected MqttSessionState()
    {
        receivedQos2 = new();
        outgoingState = new(); //TODO: investigate performance with explicit capacity initially set here
        idPool = new BitSetIdentifierPool();
    }

    [MethodImpl(AggressiveInlining)]
    public ushort RentId() => idPool.Rent();

    [MethodImpl(AggressiveInlining)]
    public void ReturnId(ushort id) => idPool.Return(id);

    public bool TryAddQoS2(ushort packetId) => receivedQos2.Add(packetId);

    public bool RemoveQoS2(ushort packetId) => receivedQos2.Remove(packetId);

    protected ushort CreateDeliveryStateCore(TPubState state)
    {
        var id = idPool.Rent();
        outgoingState.AddOrUpdate(id, (id, state));
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
        outgoingState.AddOrUpdate(packetId, state);
    }

    /// <summary>
    /// Acknowledges application message delivery and discard all associated state data
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    /// <returns><value>True</value> when delivery state existed for specified
    /// <paramref name="packetId" />
    /// , otherwise <value>False</value></returns>
    protected bool DiscardDeliveryStateCore(ushort packetId)
    {
        if (!outgoingState.TryRemove(packetId, out _)) return false;
        idPool.Return(packetId);
        return true;
    }

    public void DispatchPendingMessages([NotNull] Action<ushort, TPubState> publishHandler)
    {
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