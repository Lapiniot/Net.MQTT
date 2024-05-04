using System.Net.Mqtt.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

public sealed partial class GenericMqttHostService(IMqttServer server,
    IHostApplicationLifetime applicationLifetime,
    ILogger<GenericMqttHostService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await applicationLifetime.WaitForApplicationStartedAsync(stoppingToken).ConfigureAwait(false);
            LogStarted();

            using (server.GetFeature<IPerformanceMetricsFeature>()?.RegisterMeter())
            {
                await server.RunAsync(applicationLifetime.ApplicationStopping).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            LogError(exception);
            throw;
        }
        finally
        {
            LogStopped();
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        LogStarting();
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        LogStopping();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}