namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState4;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SubscribeBenchmarks
{
    private readonly MqttServerSessionSubscriptionState4V1 stateV1 = new();
    private readonly Server.Protocol.V3.MqttServerSessionSubscriptionState4 stateCurrent = new();
    private readonly (byte[] Filter, byte QoS)[][] data = [
        [
            ("test/topic1"u8.ToArray(), 0),
            ("test/topic2"u8.ToArray(), 0),
            ("test/topic3"u8.ToArray(), 0),
            ("test/topic4"u8.ToArray(), 0),
            ("test/topic5"u8.ToArray(), 0),
            ("test/topic6"u8.ToArray(), 0)
        ],
        [
            ("test/topic1/+"u8.ToArray(), 1),
            ("test/topic2/+"u8.ToArray(), 1),
            ("test/topic3/+"u8.ToArray(), 1),
            ("test/topic4/+"u8.ToArray(), 1),
            ("test/topic5/+"u8.ToArray(), 1),
            ("test/topic6/+"u8.ToArray(), 1)
        ],
        [
            ("test/topic1/#"u8.ToArray(), 2),
            ("test/topic2/#"u8.ToArray(), 2),
            ("test/topic3/#"u8.ToArray(), 2),
            ("test/topic4/#"u8.ToArray(), 2),
            ("test/topic5/#"u8.ToArray(), 2),
            ("test/topic6/#"u8.ToArray(), 2)
        ]
    ];

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Streaming")]
    public void SubscribeV1()
    {
        foreach (var subscriptions in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateV1.Subscribe([subscription], out _);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeV1Batch()
    {
        foreach (var subscriptions in data)
        {
            stateV1.Subscribe(subscriptions, out _);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public void SubscribeCurrent()
    {
        foreach (var subscriptions in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateCurrent.Subscribe([subscription], out _);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeCurrentBatch()
    {
        foreach (var subscriptions in data)
        {
            stateCurrent.Subscribe(subscriptions, out _);
        }
    }
}