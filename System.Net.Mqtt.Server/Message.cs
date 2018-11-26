namespace System.Net.Mqtt.Server
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

        public void Deconstruct(out string topic, out Memory<byte> payload, out QoSLevel qoSLevel)
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