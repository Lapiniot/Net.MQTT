namespace System.Net.Mqtt
{
    public abstract class MqttPacket
    {
        public abstract Memory<byte> GetBytes();
    }
}