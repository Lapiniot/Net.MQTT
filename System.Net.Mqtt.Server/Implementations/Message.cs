namespace System.Net.Mqtt.Server.Implementations
{
    public class Message
    {
        public Message(string topic, in Memory<byte> payload, QoSLevel qoSLevel, bool retain)
        {
            Topic = topic;
            Payload = payload;
            QoSLevel = qoSLevel;
            Retain = retain;
        }

        public string Topic { get; }
        public Memory<byte> Payload { get; }
        public QoSLevel QoSLevel { get; }
        public bool Retain { get; }

        public void Deconstruct(out string topic, out Memory<byte> payload, out QoSLevel qoSLevel, out bool retain)
        {
            topic = Topic;
            payload = Payload;
            qoSLevel = QoSLevel;
            retain = Retain;
        }
    }
}