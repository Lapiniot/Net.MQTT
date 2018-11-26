namespace System.Net.Mqtt.Server
{
    public class Message
    {
        public Message(string topic, in Memory<byte> payload, byte qoSLevel, bool retain)
        {
            Topic = topic;
            Payload = payload;
            QoSLevel = qoSLevel;
            Retain = retain;
        }

        public string Topic { get; }
        public Memory<byte> Payload { get; }
        public byte QoSLevel { get; }
        public bool Retain { get; }

        public void Deconstruct(out string topic, out Memory<byte> payload, out byte qoSLevel, out bool retain)
        {
            topic = Topic;
            payload = Payload;
            qoSLevel = QoSLevel;
            retain = Retain;
        }

        public void Deconstruct(out string topic, out Memory<byte> payload, out byte qoSLevel)
        {
            topic = Topic;
            payload = Payload;
            qoSLevel = QoSLevel;
        }

        public void Deconstruct(out string topic, out Memory<byte> payload)
        {
            topic = Topic;
            payload = Payload;
        }
    }
}