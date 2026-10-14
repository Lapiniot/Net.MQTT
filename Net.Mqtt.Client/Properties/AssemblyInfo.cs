global using UserProperty = (System.ReadOnlyMemory<byte> Name, System.ReadOnlyMemory<byte> Value);
global using PublishBlock = (System.ReadOnlyMemory<byte> Topic, System.ReadOnlyMemory<byte> Payload, uint Flags);

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("Net.Mqtt.Client.Tests")]
