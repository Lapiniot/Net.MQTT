namespace Net.Mqtt;

internal struct AliasTopicMap
{
    private Dictionary<ushort, ReadOnlyMemory<byte>> map;
    private ushort aliasMaximum;

    public readonly void GetOrUpdateTopic(ushort alias, ref ReadOnlyMemory<byte> topic)
    {
        if (alias is 0 || alias > aliasMaximum)
        {
            InvalidTopicAliasException.Throw();
        }

        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(map, alias, out var exists);

        if (topic.Length is not 0)
        {
            value = topic;
        }
        else if (exists)
        {
            topic = value;
        }
        else
        {
            ProtocolErrorException.Throw();
        }
    }

    public void Initialize(ushort aliasMaximum)
    {
        this.aliasMaximum = aliasMaximum;
        (map ??= []).Clear();
    }
}