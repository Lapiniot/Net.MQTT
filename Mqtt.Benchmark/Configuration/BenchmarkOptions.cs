using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt;

namespace Mqtt.Benchmark.Configuration;

public class BenchmarkOptions
{
    public Uri Server { get; set; }
    public string ClientId { get; set; }
    public int? NumMessages { get; set; }
    public int? NumClients { get; set; }
    public int? NumSubscriptions { get; set; }
    public QoSLevel? QoSLevel { get; set; }
    public TimeSpan? TimeoutOverall { get; set; }
    public TimeSpan? UpdateInterval { get; set; }
    public bool? NoProgress { get; set; }
    public string TestProfile { get; set; }
    public string TestKind { get; set; }
    public IDictionary<string, TestProfile> Profiles { get; } = new Dictionary<string, TestProfile>();
    public int? MaxConcurrent { get; set; }
    public int? MinPayloadSize { get; set; }
    public int? MaxPayloadSize { get; set; }

    public TestProfile BuildProfile() =>
        TestProfile is not null
            ? Profiles.TryGetValue(TestProfile, out var profile)
                ? new TestProfile(TestKind ?? profile.Kind, NumMessages ?? profile.NumMessages,
                    NumClients ?? profile.NumClients, NumSubscriptions ?? profile.NumSubscriptions,
                    QoSLevel ?? profile.QoSLevel, TimeoutOverall ?? profile.TimeoutOverall,
                    UpdateInterval ?? profile.UpdateInterval, NoProgress ?? profile.NoProgress, MaxConcurrent ?? profile.MaxConcurrent,
                    MinPayloadSize ?? profile.MinPayloadSize, MaxPayloadSize ?? profile.MaxPayloadSize)
                : ThrowMissingConfig()
            : Profiles.TryGetValue("Defaults", out var defaults)
                ? new(TestKind ?? defaults.Kind, NumMessages ?? defaults.NumMessages,
                    NumClients ?? defaults.NumClients, NumSubscriptions ?? defaults.NumSubscriptions,
                    QoSLevel ?? defaults.QoSLevel, TimeoutOverall ?? defaults.TimeoutOverall,
                    UpdateInterval ?? defaults.UpdateInterval, NoProgress ?? defaults.NoProgress, MaxConcurrent ?? defaults.MaxConcurrent,
                    MinPayloadSize ?? defaults.MinPayloadSize, MaxPayloadSize ?? defaults.MaxPayloadSize)
                : new TestProfile();

    [DoesNotReturn]
    private TestProfile ThrowMissingConfig() =>
        throw new ArgumentException($"Test profile '{TestProfile}' has no configuration.");
}

public record TestProfile(string Kind, int NumMessages, int NumClients, int NumSubscriptions, QoSLevel QoSLevel,
    TimeSpan TimeoutOverall, TimeSpan UpdateInterval, bool NoProgress, int? MaxConcurrent, int MinPayloadSize, int MaxPayloadSize)
{
    public TestProfile() : this("publish", 100, 1, 0, QoSLevel.QoS0, TimeSpan.FromMinutes(2),
        TimeSpan.FromMilliseconds(200), false, null, 64, 64)
    { }
}