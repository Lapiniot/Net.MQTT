using System.Net.Mqtt;

namespace Mqtt.Benchmark.Configuration;

public class BenchmarkOptions
{
    public Uri Server { get; set; }
    public string ClientId { get; set; }
    public int? NumMessages { get; set; }
    public int? NumClients { get; set; }
    public QoSLevel? QoSLevel { get; set; }
    public TimeSpan? TimeoutOverall { get; set; }
    public TimeSpan? UpdateInterval { get; set; }
    public bool? NoProgress { get; set; }
    public string TestProfile { get; set; }
    public string TestKind { get; set; }
    public IDictionary<string, TestProfile> Profiles { get; } = new Dictionary<string, TestProfile>();

    public TestProfile BuildProfile()
    {
        return TestProfile is not null
            ? Profiles.TryGetValue(TestProfile, out var profile)
                ? new TestProfile(profile.Kind, NumMessages ?? profile.NumMessages, NumClients ?? profile.NumClients,
                    QoSLevel ?? profile.QoSLevel, TimeoutOverall ?? profile.TimeoutOverall,
                    UpdateInterval ?? profile.UpdateInterval, NoProgress ?? profile.NoProgress)
                : throw new ArgumentException($"Test profile '{TestProfile}' has no configuration")
            : Profiles.TryGetValue("Defaults", out var defaults)
                ? new TestProfile(TestKind ?? defaults.Kind, NumMessages ?? defaults.NumMessages, NumClients ?? defaults.NumClients,
                    QoSLevel ?? defaults.QoSLevel, TimeoutOverall ?? defaults.TimeoutOverall,
                    UpdateInterval ?? defaults.UpdateInterval, NoProgress ?? defaults.NoProgress)
                : new TestProfile();
    }
}

public record class TestProfile(string Kind, int NumMessages, int NumClients, QoSLevel QoSLevel, TimeSpan TimeoutOverall, TimeSpan UpdateInterval, bool NoProgress)
{
    public TestProfile() : this("publish", 100, 1, QoSLevel.QoS0, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(2), false) { }
}