using Net.Mqtt.Exceptions;

namespace Net.Mqtt.Benchmarks.AliasTopicMap;

internal struct AliasTopicMap
{
    private Dictionary<ushort, ReadOnlyMemory<byte>> map;
    private ushort topicAliasMaximum;

    //[MethodImpl(AggressiveInlining)]
    public readonly void GetOrUpdateTopic(ushort alias, ref ReadOnlyMemory<byte> topic)
    {
        if (alias is 0 || alias > topicAliasMaximum)
            InvalidTopicAliasException.Throw();

        if (topic.Length is not 0)
        {
            map[alias] = topic;
        }
        else if (!map.TryGetValue(alias, out topic))
        {
            ProtocolErrorException.Throw();
        }
    }

    public void Initialize(ushort topicAliasMaximum)
    {
        this.topicAliasMaximum = topicAliasMaximum;
        (map ??= []).Clear();
    }
}