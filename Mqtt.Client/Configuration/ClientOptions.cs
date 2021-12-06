namespace Mqtt.Client.Configuration;

public class ClientOptions
{
    public Uri Server { get; set; } = new Uri("tcp://127.0.0.1:1883");
    public string ClientId { get; set; }
}