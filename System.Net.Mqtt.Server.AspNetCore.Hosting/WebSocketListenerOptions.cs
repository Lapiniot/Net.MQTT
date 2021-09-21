namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public class WebSocketListenerOptions
{
    public WebSocketListenerOptions()
    {
        AcceptRules = new Dictionary<string, string[]>();
        QueueCapacity = 50;
    }

    public IDictionary<string, string[]> AcceptRules { get; }

    public int QueueCapacity { get; set; }
}