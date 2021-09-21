namespace System.Net.Mqtt.Server;

public record Message(string Topic, in Memory<byte> Payload, byte QoSLevel, bool Retain)
{
    public void Deconstruct(out string topic, out Memory<byte> payload, out byte qoSLevel)
    {
        topic = Topic;
        payload = Payload;
        qoSLevel = QoSLevel;
    }
}