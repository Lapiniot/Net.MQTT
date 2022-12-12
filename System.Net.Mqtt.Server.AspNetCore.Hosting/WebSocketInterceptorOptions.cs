namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public class WebSocketInterceptorOptions
{
    public WebSocketInterceptorOptions()
    {
        AcceptProtocols = new Dictionary<string, string[]>();
        QueueCapacity = 50;
    }

    public IDictionary<string, string[]> AcceptProtocols { get; }

    public int QueueCapacity { get; set; }
}