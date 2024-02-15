namespace Net.Mqtt;

public interface IApplicationMessage
{
    ReadOnlyMemory<byte> Topic { get; }
    ReadOnlyMemory<byte> Payload { get; }
}
