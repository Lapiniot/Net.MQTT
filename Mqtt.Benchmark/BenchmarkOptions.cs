namespace Mqtt.Benchmark;

#pragma warning disable CA1812

internal sealed class BenchmarkOptions : ProfileOptions
{
    public Uri Server { get; set; } = default!;
    public Protocol Protocol { get; set; }
    public bool ForceHttp2 { get; set; }
}

internal enum Protocol
{
    Auto = 0,
    Mqtt_3_1 = 3,
    Mqtt_3_1_1 = 4,
    Mqtt5 = 5,
    Level3 = Mqtt_3_1,
    Level4 = Mqtt_3_1_1,
    Level5 = Mqtt5
}

internal class ProfileOptions
{
    public string Kind { get; set; } = "publish";

    public int NumMessages { get; set; } = 100;

    public int NumClients { get; set; } = 1;

    public int NumSubscriptions { get; set; }

    public QoSLevel QoSLevel { get; set; } = QoSLevel.QoS0;

    public TimeSpan TimeoutOverall { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMilliseconds(200);

    public bool NoProgress { get; set; }

    public int? MaxConcurrent { get; set; }

    public int MinPayloadSize { get; set; } = 64;

    public int MaxPayloadSize { get; set; } = 64;
}