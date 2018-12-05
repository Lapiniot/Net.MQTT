namespace System.Net.Mqtt.Client
{
    public class MqttMessage
    {
        public MqttMessage(string topic, Memory<byte> payload, bool retained)
        {
            Topic = topic;
            Payload = payload;
            Retained = retained;
        }

        public string Topic { get; }
        public Memory<byte> Payload { get; }
        public bool Retained { get; }
    }
}