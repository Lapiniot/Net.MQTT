using System.Text;

namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class TopicMatchesBenchmarks
{
    private readonly MqttServerSessionSubscriptionState5V1 stateV1 = new();
    private readonly Server.Protocol.V5.MqttServerSessionSubscriptionState5 stateCurrent = new();
    private readonly byte[] matchingTopic = Encoding.UTF8.GetBytes("test/topic3");
    private readonly byte[] nonMatchingTopic = Encoding.UTF8.GetBytes("test/topic8");

    [GlobalSetup]
    public void Setup()
    {
        stateV1.Subscribe([
            ("test/topic1"u8.ToArray(), 0),
            ("test/topic2"u8.ToArray(), 0),
            ("test/topic3"u8.ToArray(), 0),
            ("test/topic4"u8.ToArray(), 0),
            ("test/topic5"u8.ToArray(), 0),
            ("test/topic6"u8.ToArray(), 0)
        ], subscriptionId: 42);

        stateV1.Subscribe([
            ("test/topic1/+"u8.ToArray(), 1),
            ("test/topic2/+"u8.ToArray(), 1),
            ("test/topic3/+"u8.ToArray(), 1),
            ("test/topic4/+"u8.ToArray(), 1),
            ("test/topic5/+"u8.ToArray(), 1),
            ("test/topic6/+"u8.ToArray(), 1),
        ], subscriptionId: 45);

        stateV1.Subscribe([
            ("test/topic1/#"u8.ToArray(), 2),
            ("test/topic2/#"u8.ToArray(), 2),
            ("test/topic3/#"u8.ToArray(), 2),
            ("test/topic4/#"u8.ToArray(), 2),
            ("test/topic5/#"u8.ToArray(), 2),
            ("test/topic6/#"u8.ToArray(), 2),
        ], subscriptionId: 48);

        stateCurrent.Subscribe([
            ("test/topic1"u8.ToArray(), 0),
            ("test/topic2"u8.ToArray(), 0),
            ("test/topic3"u8.ToArray(), 0),
            ("test/topic4"u8.ToArray(), 0),
            ("test/topic5"u8.ToArray(), 0),
            ("test/topic6"u8.ToArray(), 0)
        ], subscriptionId: 42);

        stateCurrent.Subscribe([
            ("test/topic1/+"u8.ToArray(), 1),
            ("test/topic2/+"u8.ToArray(), 1),
            ("test/topic3/+"u8.ToArray(), 1),
            ("test/topic4/+"u8.ToArray(), 1),
            ("test/topic5/+"u8.ToArray(), 1),
            ("test/topic6/+"u8.ToArray(), 1),
        ], subscriptionId: 45);

        stateCurrent.Subscribe([
            ("test/topic1/#"u8.ToArray(), 2),
            ("test/topic2/#"u8.ToArray(), 2),
            ("test/topic3/#"u8.ToArray(), 2),
            ("test/topic4/#"u8.ToArray(), 2),
            ("test/topic5/#"u8.ToArray(), 2),
            ("test/topic6/#"u8.ToArray(), 2),
        ], subscriptionId: 48);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Matching")]
    public void TopicMatchesMatchingV1()
    {
        stateV1.TopicMatches(matchingTopic, out _, out _);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("NonMatching")]
    public void TopicMatchesNonMatchingV1()
    {
        stateV1.TopicMatches(nonMatchingTopic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("Matching")]
    public void TopicMatchesMatchingCurrent()
    {
        stateCurrent.TopicMatches(matchingTopic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("NonMatching")]
    public void TopicMatchesNonMatchingCurrent()
    {
        stateCurrent.TopicMatches(nonMatchingTopic, out _, out _);
    }
}