global using UserProperty = (System.ReadOnlyMemory<byte> Name, System.ReadOnlyMemory<byte> Value);

[assembly: InternalsVisibleTo("Net.Mqtt.Tests")]
[assembly: InternalsVisibleTo("Net.Mqtt.Benchmarks")]
[assembly: InternalsVisibleTo("Net.Mqtt.Client")]
[assembly: InternalsVisibleTo("Net.Mqtt.Server")]
[assembly: CLSCompliant(false)]
