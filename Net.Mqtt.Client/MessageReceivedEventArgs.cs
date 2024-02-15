namespace Net.Mqtt.Client;

public class MessageReceivedEventArgs(string topic, ReadOnlyMemory<byte> payload, bool retained) : EventArgs
{
    public string Topic { get; } = topic;
    public ReadOnlyMemory<byte> Payload { get; } = payload;
    public bool Retained { get; } = retained;
}