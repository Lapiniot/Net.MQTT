namespace System.Net.Mqtt.Server;

public interface IMqttServer : IAsyncDisposable
{
    Task RunAsync(CancellationToken stoppingToken);
    Task StopAsync();
}