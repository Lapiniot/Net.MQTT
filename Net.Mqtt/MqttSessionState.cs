using System.Collections;

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
    private readonly OrderedDictionary<ushort, TPubState> pubState;
    private readonly HashSet<ushort> receivedQos2;

    public MqttSessionState()
    {
        receivedQos2 = [];
        pubState = []; //TODO: investigate performance with explicit capacity initially set here
        idPool = new BitSetIdentifierPool();
    }

    public ushort RentId() => idPool.Rent();

    public void ReturnId(ushort id) => idPool.Return(id);

    public bool TryAddQoS2(ushort packetId) => receivedQos2.Add(packetId);

    public bool RemoveQoS2(ushort packetId) => receivedQos2.Remove(packetId);

    public ushort CreateMessageDeliveryState(in TPubState state)
    {
        var id = idPool.Rent();
        lock (pubState)
        {
            pubState.Add(id, state);
        }

        return id;
    }

    /// <summary>
    /// Updates QoS 2 message delivery state data to indicate PUBLISH packet has been acknowledged
    /// (in response to the corresponding PUBREC packet)
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    /// <returns><see langword="true" /> if delivery state has been successfully marked as acknowledged 
    /// for existing <paramref name="packetId"/></returns>
    public bool SetMessagePublishAcknowledged(ushort packetId)
    {
        lock (pubState)
        {
            var index = pubState.IndexOf(packetId);
            if (index == -1) return false;
            pubState.SetAt(index, default);
            return true;
        }
    }

    /// <summary>
    /// Acknowledges application message delivery and discard all associated state data
    /// </summary>
    /// <param name="packetId">Packet Id associated with this protocol exchange</param>
    /// <returns><value>True</value> when delivery state existed for specified
    /// <paramref name="packetId" />
    /// , otherwise <value>False</value></returns>
    public bool DiscardMessageDeliveryState(ushort packetId)
    {
        lock (pubState)
        {
            if (!pubState.Remove(packetId, out _)) return false;
        }

        idPool.Return(packetId);
        return true;
    }

    public PublishStateEnumerator PublishState => new(pubState);

    public virtual void Trim()
    {
        receivedQos2.TrimExcess();
        pubState.TrimExcess();
    }

    public struct PublishStateEnumerator :
        IEnumerable<KeyValuePair<ushort, TPubState>>,
        IEnumerator<KeyValuePair<ushort, TPubState>>
    {
        private const int Initialized = -4;
        private const int Initializing = -3;
        private const int NotReady = -2;
        private const int Done = -1;
        private const int BeforeInit = 0;
        private const int Progressing = 1;

        private readonly OrderedDictionary<ushort, TPubState> map;
        private OrderedDictionary<ushort, TPubState>.Enumerator enumerator;
        private int state;
        private bool locked;

        internal PublishStateEnumerator(OrderedDictionary<ushort, TPubState> map)
        {
            this.map = map;
            state = NotReady;
        }

        public PublishStateEnumerator GetEnumerator() => new(map) { state = BeforeInit };

        IEnumerator<KeyValuePair<ushort, TPubState>> IEnumerable<KeyValuePair<ushort, TPubState>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public KeyValuePair<ushort, TPubState> Current { get; private set; }

        readonly object IEnumerator.Current => Current;

        public void Dispose()
        {
            if (state is Done)
                return;

            try
            {
                if (state is Progressing or Initialized)
                {
                    Deinit();
                }
            }
            finally
            {
                Exit();
            }
        }

        public bool MoveNext()
        {
            try
            {
                var local = state;
                if (local is not BeforeInit)
                {
                    if (local is not Progressing)
                        return false;
                }
                else
                {
                    state = Initializing;
                    Monitor.Enter(map, ref locked);
                    enumerator = map.GetEnumerator();
                }

                state = Initialized;

                if (enumerator.MoveNext())
                {
                    Current = enumerator.Current;
                    state = Progressing;
                    return true;
                }

                Deinit();
                Exit();
                return false;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Deinit()
        {
            state = Initializing;
            enumerator = default;
        }

        private void Exit()
        {
            state = Done;
            if (locked)
            {
                Monitor.Exit(map);
            }
        }

        public void Reset() => throw new NotSupportedException();
    }
}