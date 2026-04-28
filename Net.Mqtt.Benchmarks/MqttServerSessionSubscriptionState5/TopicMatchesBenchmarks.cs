namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

[MemoryDiagnoser]
[CategoriesColumn]
[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class TopicMatchesBenchmarks
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

    public IEnumerable<Sample<byte[]>> Topics
    {
        get
        {
            yield return new("Matching", "test/topic3/1"u8.ToArray());
            yield return new("NonMatching", "test/nomatch"u8.ToArray());
        }
    }

    [ParamsSource(nameof(Topics))]
    public Sample<byte[]> Topic { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        foreach (var (subscriptions, id) in data)
        {
            stateV1.Subscribe(subscriptions, id);
            stateV2.Subscribe(subscriptions, id);
            stateV3.Subscribe(subscriptions, id);
            stateV4.Subscribe(subscriptions, id);
            stateV5.Subscribe(subscriptions, id);
            stateCurrent.Subscribe(subscriptions, id);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("V1", "Initial")]
    public void TopicMatchesV1()
    {
        stateV1.TopicMatches(Topic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("V2")]
    public void TopicMatchesV2()
    {
        stateV2.TopicMatches(Topic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("V3")]
    public void TopicMatchesV3()
    {
        stateV3.TopicMatches(Topic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("V4")]
    public void TopicMatchesV4()
    {
        stateV4.TopicMatches(Topic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("V5", "Prev")]
    public void TopicMatchesV5()
    {
        stateV5.TopicMatches(Topic, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("V6", "Current")]
    public void TopicMatchesCurrent()
    {
        stateCurrent.TopicMatches(Topic, out _, out _);
    }
}