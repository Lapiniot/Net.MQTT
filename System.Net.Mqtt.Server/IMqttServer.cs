namespace System.Net.Mqtt.Server;

public interface IMqttServer : IAsyncDisposable
{
    T? GetFeature<T>() where T : class;
    Task RunAsync(CancellationToken stoppingToken);
    Task StopAsync();
}