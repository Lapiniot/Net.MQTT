namespace System.Net.Mqtt.Client;

public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(string topic, ReadOnlyMemory<byte> payload, bool retained)
    {
        Topic = topic;
        Payload = payload;
        Retained = retained;
    }

    public string Topic { get; }
    public ReadOnlyMemory<byte> Payload { get; }
    public bool Retained { get; }
}