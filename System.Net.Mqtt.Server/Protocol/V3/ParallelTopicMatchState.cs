namespace System.Net.Mqtt.Server.Protocol.V3;

public class ParallelTopicMatchState
{
    private readonly Action<int> aggregate;
    private readonly Func<KeyValuePair<Utf8String, byte>, ParallelLoopState, int, int> match;
    private int maxQoS;
    private Utf8String topic;

    public ParallelTopicMatchState()
    {
        aggregate = AggregateInternal;
        match = MatchInternal;
    }

    public Utf8String Topic { get => topic; set => topic = value; }
    public int MaxQoS { get => maxQoS; set => maxQoS = value; }
    public Func<KeyValuePair<Utf8String, byte>, ParallelLoopState, int, int> Match => match;
    public Action<int> Aggregate => aggregate;

    private int MatchInternal(KeyValuePair<Utf8String, byte> pair, ParallelLoopState _, int qos)
    {
        var (filter, level) = pair;
        return MqttExtensions.TopicMatches(topic.Span, filter.Span) && level > qos ? level : qos;
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