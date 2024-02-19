namespace Net.Mqtt;

internal struct TopicAliasMap
{
    private Dictionary<ReadOnlyMemory<byte>, ushort> map;
    private ushort nextAlias;
    private ushort aliasMaximum;

    /// <summary>
    /// Initializes current instance and prepares it for new usage clearing all previouse state.
    /// </summary>
    /// <param name="aliasMaximum">
    /// Maximum alias value this instance can provide upon successful call to
    /// <see cref="TryGetAlias(ReadOnlyMemory{byte}, out KeyValuePair{ReadOnlyMemory{byte}, ushort}, out bool)"/>
    /// </param>
    public void Initialize(ushort aliasMaximum)
    {
        nextAlias = 1;
        this.aliasMaximum = aliasMaximum;
        (map ??= new(ByteSequenceComparer.Instance)).Clear();
    }

    /// <summary>
    /// Try to get alias number for given topic.
    /// </summary>
    /// <param name="topic">Topic to look alias for.</param>
    /// <param name="mapping">
    /// Topic to alias mapping either existing already
    /// for given topic or next available candidate to be created
    /// upon successful PUBLISH packet onward delivery.
    /// </param>
    /// <param name="newNeedsCommit">
    /// Alias mapping doesn't exist yet, but new alias value is
    /// available and could be used for onward PUBLISH delivery.
    /// Caller is responsible to call <see cref="Commit(ReadOnlyMemory{byte})"/>
    /// method to commit new alias mapping upon successful onward delivery
    /// for the corresponding PUBLISH packet. This creates permanent
    /// topic/alias mapping for future reuse.
    /// </param>
    /// <returns><see langword="true"/> if topic/alias mapping already exists or new one could be created, otherwise <see langword="false"/>.</returns>
    public readonly bool TryGetAlias(ReadOnlyMemory<byte> topic, out KeyValuePair<ReadOnlyMemory<byte>, ushort> mapping, out bool newNeedsCommit)
    {
        if (map.TryGetValue(topic, out var alias))
        {
            newNeedsCommit = false;
            mapping = new(default, alias);
            return true;
        }
        else if (nextAlias <= aliasMaximum)
        {
            newNeedsCommit = true;
            mapping = new(topic, nextAlias);
            return true;
        }

        newNeedsCommit = false;
        mapping = default;
        return false;
    }

    /// <summary>
    /// Creates permanent mapping for the given topic.
    /// Caller is responsible to call this method right after successful PUBLISH delivery
    /// with alias given by the call to <see cref="TryGetAlias(ReadOnlyMemory{byte}, out KeyValuePair{ReadOnlyMemory{byte}, ushort}, out bool)"/>
    /// when newNeedsCommit param is <see langword="true"/>.
    /// </summary>
    /// <param name="topic">Topic to create new mapping for.</param>
    public void Commit(ReadOnlyMemory<byte> topic) => map[topic] = nextAlias++;
}