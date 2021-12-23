using System.Net.Mqtt.Extensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

internal struct ParallelTopicMatchState
{
    private string topic;
    private int maxQoS;
    private Func<KeyValuePair<string, byte>, ParallelLoopState, int, int> match;
    private Action<int> aggregate;

    public string Topic { get => topic; set => topic = value; }
    public int MaxQoS { get => maxQoS; set => maxQoS = value; }
    public Func<KeyValuePair<string, byte>, ParallelLoopState, int, int> Match => match ??= new(MatchInternal);
    public Action<int> Aggregate => aggregate ??= new(AggregateInternal);

    private int MatchInternal(KeyValuePair<string, byte> pair, ParallelLoopState _, int qos)
    {
        var (filter, level) = pair;
        return MqttExtensions.TopicMatches(topic, filter) && level > qos ? level : qos;
    }

    private void AggregateInternal(int level)
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
}