namespace System.Net.Mqtt.Server.Hosting;

public interface IMqttServerBuilder
{
    ValueTask<IMqttServer> BuildAsync();
}