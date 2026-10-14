global using PublishBlock = (System.ReadOnlyMemory<byte> Topic, System.ReadOnlyMemory<byte> Payload, uint Flags);
global using UserProperty = (System.ReadOnlyMemory<byte> Name, System.ReadOnlyMemory<byte> Value);

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("Net.Mqtt.Server.Tests")]