namespace System.Net.Mqtt
{
    public abstract class MqttMessage
    {
        public abstract int GetSize();

        public abstract Memory<byte> GetBytes();
    }
}