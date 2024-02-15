using Microsoft.Extensions.Configuration;
using Net.Mqtt;

namespace Mqtt.Benchmark.Configuration;

[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
public class BenchmarkOptionsFactory(IConfiguration configuration) : IOptionsFactory<BenchmarkOptions>
{
    public BenchmarkOptions Create(string name)
    {
        var ts = configuration.GetSection("Profiles");
        var ds = ts.GetSection("Defaults");

        var options = new BenchmarkOptions
        {
            Server = new Uri(Environment.ExpandEnvironmentVariables(configuration.GetValue(nameof(BenchmarkOptions.Server), "tcp://localhost:1883"))),
            Protocol = configuration.GetValue(nameof(BenchmarkOptions.Protocol), Protocol.Auto),
            ForceHttp2 = configuration.GetValue(nameof(BenchmarkOptions.ForceHttp2), false),
            NumClients = configuration.GetValue<int?>(nameof(BenchmarkOptions.NumClients)),
            NumMessages = configuration.GetValue<int?>(nameof(BenchmarkOptions.NumMessages)),
            NumSubscriptions = configuration.GetValue<int?>(nameof(BenchmarkOptions.NumSubscriptions)),
            QoSLevel = configuration.GetValue<QoSLevel?>(nameof(BenchmarkOptions.QoSLevel)),
            TimeoutOverall = configuration.GetValue<TimeSpan?>(nameof(BenchmarkOptions.TimeoutOverall)),
            TestKind = configuration.GetValue<string>(nameof(BenchmarkOptions.TestKind)),
            TestProfile = configuration.GetValue<string>(nameof(BenchmarkOptions.TestProfile)),
            UpdateInterval = configuration.GetValue<TimeSpan?>(nameof(BenchmarkOptions.UpdateInterval)),
            NoProgress = configuration.GetValue<bool?>(nameof(BenchmarkOptions.NoProgress)),
            MaxConcurrent = configuration.GetValue<int?>(nameof(BenchmarkOptions.MaxConcurrent)),
            MinPayloadSize = configuration.GetValue<int?>(nameof(BenchmarkOptions.MinPayloadSize)),
            MaxPayloadSize = configuration.GetValue<int?>(nameof(BenchmarkOptions.MaxPayloadSize))
        };

        var defaults = new TestProfile(
            ds.GetValue(nameof(TestProfile.Kind), "publish"),
            ds.GetValue(nameof(TestProfile.NumMessages), 1000),
            ds.GetValue(nameof(TestProfile.NumClients), 1),
            ds.GetValue(nameof(TestProfile.NumSubscriptions), 10),
            ds.GetValue(nameof(TestProfile.QoSLevel), QoSLevel.QoS0),
            ds.GetValue(nameof(TestProfile.TimeoutOverall), TimeSpan.FromMinutes(2)),
            ds.GetValue(nameof(TestProfile.UpdateInterval), TimeSpan.FromMilliseconds(200)),
            ds.GetValue(nameof(TestProfile.NoProgress), false),
            ds.GetValue<int?>(nameof(TestProfile.MaxConcurrent)),
            ds.GetValue(nameof(TestProfile.MinPayloadSize), 64),
            ds.GetValue(nameof(TestProfile.MaxPayloadSize), 64));
        options.Profiles.Add("Defaults", defaults);

        foreach (var ps in ts.GetChildren())
        {
            if (ps is { Key: "Defaults" }) continue;
            options.Profiles.Add(ps.Key, new(
                ps.GetValue(nameof(TestProfile.Kind), defaults.Kind),
                ps.GetValue(nameof(TestProfile.NumMessages), defaults.NumMessages),
                ps.GetValue(nameof(TestProfile.NumClients), defaults.NumClients),
                ps.GetValue(nameof(TestProfile.NumSubscriptions), defaults.NumSubscriptions),
                ps.GetValue(nameof(TestProfile.QoSLevel), defaults.QoSLevel),
                ps.GetValue(nameof(TestProfile.TimeoutOverall), defaults.TimeoutOverall),
                ps.GetValue(nameof(TestProfile.UpdateInterval), defaults.UpdateInterval),
                ps.GetValue(nameof(TestProfile.NoProgress), defaults.NoProgress),
                ps.GetValue(nameof(TestProfile.MaxConcurrent), defaults.MaxConcurrent),
                ps.GetValue(nameof(TestProfile.MinPayloadSize), defaults.MinPayloadSize),
                ps.GetValue(nameof(TestProfile.MaxPayloadSize), defaults.MaxPayloadSize)));
        }

        return options;
    }
}