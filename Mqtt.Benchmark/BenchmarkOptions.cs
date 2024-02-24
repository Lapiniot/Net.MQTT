namespace Mqtt.Benchmark;

public sealed class BenchmarkOptions : ProfileOptions
{
    public Uri Server { get; set; }
    public Protocol Protocol { get; set; }
    public bool ForceHttp2 { get; set; }
    public string TestProfile { get; set; }
}

public enum Protocol
{
    Auto = 0,
#pragma warning disable CA1707
    Mqtt_3_1 = 3,
    Mqtt_3_1_1 = 4,
#pragma warning restore CA1707
    Mqtt5 = 5,
    Level3 = Mqtt_3_1,
    Level4 = Mqtt_3_1_1,
    Level5 = Mqtt5
}

public class ProfileOptions
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