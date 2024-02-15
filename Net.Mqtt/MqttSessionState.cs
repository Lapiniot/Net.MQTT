namespace Net.Mqtt;

public readonly record struct PublishDeliveryState(int Flags, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload);

/// <summary>
/// Base abstract type for MQTT session state
/// </summary>
public class MqttSessionState
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
public class MqttSessionState<TPubState> : MqttSessionState
{
    private readonly BitSetIdentifierPool idPool;
    private readonly OrderedHashMap<ushort, TPubState> outgoingState;
    private readonly HashSet<ushort> receivedQos2;

    public MqttSessionState()
    {
        receivedQos2 = [];
        outgoingState = new(); //TODO: investigate performance with explicit capacity initially set here
        idPool = new BitSetIdentifierPool();
    }

    public ushort RentId() => idPool.Rent();

    public void ReturnId(ushort id) => idPool.Return(id);

    public bool TryAddQoS2(ushort packetId) => receivedQos2.Add(packetId);

    public bool RemoveQoS2(ushort packetId) => receivedQos2.Remove(packetId);

    public ushort CreateMessageDeliveryState(in TPubState state)
    {
        var id = idPool.Rent();
        outgoingState.AddOrUpdate(id, state);
        return id;
    }

    /// <summary>
    /// Updates QoS 2 message delivery state data to indicate PUBLISH packet has been acknowledged
    /// (in response to the corresponding PUBREC packet)
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    /// <returns><see langword="true" /> if delivery state has been successfully marked as acknowledged 
    /// for existing <paramref name="packetId"/></returns>
    public bool SetMessagePublishAcknowledged(ushort packetId) => outgoingState.Update(packetId, default);

    /// <summary>
    /// Acknowledges application message delivery and discard all associated state data
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    /// <returns><value>True</value> when delivery state existed for specified
    /// <paramref name="packetId" />
    /// , otherwise <value>False</value></returns>
    public bool DiscardMessageDeliveryState(ushort packetId)
    {
        if (!outgoingState.Remove(packetId, out _)) return false;
        idPool.Return(packetId);
        return true;
    }

    public OutgoingPublishState PublishState => new(outgoingState);

    public virtual void Trim()
    {
        receivedQos2.TrimExcess();
        outgoingState.TrimExcess();
    }

#pragma warning disable CA1034

    public readonly struct OutgoingPublishState : IEquatable<OutgoingPublishState>
    {
        private readonly OrderedHashMap<ushort, TPubState> hashMap;

        internal OutgoingPublishState(OrderedHashMap<ushort, TPubState> hashMap) => this.hashMap = hashMap;

        public OrderedHashMap<ushort, TPubState>.Enumerator GetEnumerator() => hashMap.GetEnumerator();

        public override bool Equals(object obj) => obj is OutgoingPublishState other && hashMap == other.hashMap;

        public override int GetHashCode() => hashMap.GetHashCode();

        public bool Equals(OutgoingPublishState other) => hashMap == other.hashMap;

        public static bool operator ==(OutgoingPublishState left, OutgoingPublishState right) => left.hashMap == right.hashMap;

        public static bool operator !=(OutgoingPublishState left, OutgoingPublishState right) => left.hashMap != right.hashMap;
    }

#pragma warning restore CA1034
}