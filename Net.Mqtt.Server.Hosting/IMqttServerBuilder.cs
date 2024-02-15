namespace Net.Mqtt.Server.Hosting;

public interface IMqttServerBuilder
{
    IMqttServer Build();
}