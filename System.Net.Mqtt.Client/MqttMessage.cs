namespace System.Net.Mqtt.Client
{
    public class MqttMessage
    {
        public MqttMessage(string topic, Memory<byte> payload)
        {
            Topic = topic;
            Payload = payload;
        }

        public string Topic { get; set; }
        public Memory<byte> Payload { get; set; }
    }
}