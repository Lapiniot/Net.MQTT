using System.Net.Mqtt.Extensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class ParallelTopicMatchState
{
    private string topic;
    private int maxQoS;
    private readonly Func<KeyValuePair<string, byte>, ParallelLoopState, int, int> match;
    private readonly Action<int> aggregate;

    public ParallelTopicMatchState()
    {
        aggregate = AggregateInternal;
        match = MatchInternal;
    }

    public string Topic { get => topic; set => topic = value; }
    public int MaxQoS { get => maxQoS; set => maxQoS = value; }
    public Func<KeyValuePair<string, byte>, ParallelLoopState, int, int> Match => match;
    public Action<int> Aggregate => aggregate;

    private int MatchInternal(KeyValuePair<string, byte> pair, ParallelLoopState _, int qos)
    {
        var (filter, level) = pair;
        return MqttExtensions.TopicMatches(topic, filter) && level > qos ? level : qos;
    }

    private void AggregateInternal(int level)
    {
        var current = Volatile.Read(ref maxQoS);
        for (var i = current; i < level; i++)
        {
            var value = Interlocked.CompareExchange(ref maxQoS, level, i);
            if (value == i || value >= level)
            {
                break;
            }
        }
    }
}