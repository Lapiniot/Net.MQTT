namespace System.Net.Mqtt
{
    public abstract class MqttMessage
    {
        public virtual QoSLevel QoSLevel { get; set; }

        public virtual bool Duplicate { get; set; }

        public virtual bool Retain { get; set; }

        public abstract Memory<byte> GetBytes();
    }
}