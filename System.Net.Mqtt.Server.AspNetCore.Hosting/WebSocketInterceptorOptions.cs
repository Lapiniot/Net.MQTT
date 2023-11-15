namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public sealed class WebSocketInterceptorOptions
{
    public Dictionary<string, string[]> AcceptProtocols { get; } = [];

    public int QueueCapacity { get; set; } = 50;
}