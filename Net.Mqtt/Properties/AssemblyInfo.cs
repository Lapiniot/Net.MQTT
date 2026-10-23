global using UserProperty = (System.ReadOnlyMemory<byte> Name, System.ReadOnlyMemory<byte> Value);
global using PacketWriteResult = (int BytesWritten, Net.Mqtt.PacketType PacketType);
global using PublishBlock = (System.ReadOnlyMemory<byte> Topic, System.ReadOnlyMemory<byte> Payload, uint Flags);

[assembly: InternalsVisibleTo("Net.Mqtt.Tests")]
[assembly: InternalsVisibleTo("Net.Mqtt.Benchmarks")]
[assembly: InternalsVisibleTo("Net.Mqtt.Client")]
[assembly: InternalsVisibleTo("Net.Mqtt.Server")]
[assembly: CLSCompliant(false)]
