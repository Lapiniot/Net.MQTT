global using UserProperty = (System.ReadOnlyMemory<byte> Name, System.ReadOnlyMemory<byte> Value);

[assembly: InternalsVisibleTo("System.Net.Mqtt.Tests")]
[assembly: InternalsVisibleTo("System.Net.Mqtt.Benchmarks")]
[assembly: InternalsVisibleTo("System.Net.Mqtt.Client")]
[assembly: InternalsVisibleTo("System.Net.Mqtt.Server")]
[assembly: CLSCompliant(false)]
