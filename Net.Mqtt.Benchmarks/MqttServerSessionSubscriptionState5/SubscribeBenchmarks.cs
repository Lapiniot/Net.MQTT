namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SubscribeBenchmarks
{
    private readonly MqttServerSessionSubscriptionState5V1 stateV1 = new();
    private readonly MqttServerSessionSubscriptionState5V2 stateV2 = new();
    private readonly MqttServerSessionSubscriptionState5V3 stateV3 = new();
    private readonly MqttServerSessionSubscriptionState5V4 stateV4 = new();
    private readonly MqttServerSessionSubscriptionState5V5 stateV5 = new();
    private readonly Server.Protocol.V5.MqttServerSessionSubscriptionState5 stateCurrent = new();
    private readonly ((byte[] Filter, byte Flags)[], uint)[] data = [
        ([
            ("test/topic1"u8.ToArray(), 0),
            ("test/topic2"u8.ToArray(), 0),
            ("test/topic3"u8.ToArray(), 0),
            ("test/topic4"u8.ToArray(), 0),
            ("test/topic5"u8.ToArray(), 0),
            ("test/topic6"u8.ToArray(), 0)
        ], 42),
        ([
            ("test/topic1/+"u8.ToArray(), 1),
            ("test/topic2/+"u8.ToArray(), 1),
            ("test/topic3/+"u8.ToArray(), 1),
            ("test/topic4/+"u8.ToArray(), 1),
            ("test/topic5/+"u8.ToArray(), 1),
            ("test/topic6/+"u8.ToArray(), 1)
        ], 45),
        ([
            ("test/topic1/#"u8.ToArray(), 2),
            ("test/topic2/#"u8.ToArray(), 2),
            ("test/topic3/#"u8.ToArray(), 2),
            ("test/topic4/#"u8.ToArray(), 2),
            ("test/topic5/#"u8.ToArray(), 2),
            ("test/topic6/#"u8.ToArray(), 2)
        ], 48)
    ];

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Streaming")]
    public void SubscribeV1()
    {
        foreach (var (subscriptions, id) in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateV1.Subscribe([subscription], id);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public void SubscribeV2()
    {
        foreach (var (subscriptions, id) in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateV2.Subscribe([subscription], id);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public void SubscribeV3()
    {
        foreach (var (subscriptions, id) in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateV3.Subscribe([subscription], id);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public void SubscribeV4()
    {
        foreach (var (subscriptions, id) in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateV4.Subscribe([subscription], id);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public void SubscribeV5()
    {
        foreach (var (subscriptions, id) in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateV5.Subscribe([subscription], id);
            }
        }
    }

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public void SubscribeCurrent()
    {
        foreach (var (subscriptions, id) in data)
        {
            foreach (var subscription in subscriptions)
            {
                stateCurrent.Subscribe([subscription], id);
            }
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Batching")]
    public void SubscribeV1Batch()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateV1.Subscribe(subscriptions, id);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeV2Batch()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateV2.Subscribe(subscriptions, id);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeV3Batch()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateV3.Subscribe(subscriptions, id);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeV4Batch()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateV4.Subscribe(subscriptions, id);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeV5Batch()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateV5.Subscribe(subscriptions, id);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batching")]
    public void SubscribeCurrentBatch()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateCurrent.Subscribe(subscriptions, id);
        }
    }
}