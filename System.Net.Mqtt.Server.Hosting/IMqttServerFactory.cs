namespace System.Net.Mqtt.Server.Hosting
{
    public interface IMqttServerFactory
    {
        MqttServer Create();
    }
}