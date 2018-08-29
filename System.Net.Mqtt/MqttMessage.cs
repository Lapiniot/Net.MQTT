namespace System.Net.Mqtt
{
    public abstract class MqttMessage
    {
        public QoSLevel QoSLevel { get; set; }

        public bool Duplicate { get; set; }

        public bool Retain { get; set; }

        public abstract Memory<byte> GetBytes();
    }
}