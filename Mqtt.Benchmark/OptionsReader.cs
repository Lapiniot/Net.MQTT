using System.Net.Mqtt;
using Microsoft.Extensions.Configuration;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static class OptionsReader
{
    internal static BenchmarkOptions Read(IConfigurationRoot configuration)
    {
        var ts = configuration.GetSection("Profiles");
        var ds = ts.GetSection("Defaults");

        var options = new BenchmarkOptions()
        {
            Server = configuration.GetValue("Server", new Uri("tcp://127.0.0.1:1883")),
            ClientId = configuration.GetValue<string>("ClientId"),
            NumClients = configuration.GetValue<int?>("NumClients"),
            NumMessages = configuration.GetValue<int?>("NumMessages"),
            QoSLevel = configuration.GetValue<QoSLevel?>("QoSLevel"),
            TimeoutOverall = configuration.GetValue<TimeSpan?>("TimeoutOverall"),
            TestKind = configuration.GetValue<string>("TestKind"),
            TestProfile = configuration.GetValue<string>("TestProfile")
        };

        var defaults = new TestProfile(ds.GetValue("Kind", "publish"),
            ds.GetValue("NumMessages", 1000),
            ds.GetValue("NumClients", 1),
            ds.GetValue("QoSLevel", QoSLevel.QoS0),
            ds.GetValue("TimeoutOverall", TimeSpan.FromMinutes(2)));
        options.Profiles.Add("Defaults", defaults);

        foreach(var ps in ts.GetChildren())
        {
            if(ps is { Key: "Defaults" }) continue;
            options.Profiles.Add(ps.Key, new TestProfile(
                ps.GetValue("Kind", defaults.Kind),
                ps.GetValue("NumMessages", defaults.NumMessages),
                ps.GetValue("NumClients", defaults.NumClients),
                ps.GetValue("QoSLevel", defaults.QoSLevel),
                ps.GetValue("TimeoutOverall", defaults.TimeoutOverall)));
        }

        return options;
    }
}