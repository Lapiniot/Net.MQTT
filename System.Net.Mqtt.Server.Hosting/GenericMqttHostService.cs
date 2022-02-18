using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

public sealed partial class GenericMqttHostService : BackgroundService
{
    private readonly IMqttServerBuilder serverBuilder;
    private readonly IHostApplicationLifetime applicationLifetime;

    public GenericMqttHostService(IMqttServerBuilder serverBuilder, IHostApplicationLifetime applicationLifetime, ILogger<GenericMqttHostService> logger)
    {
        this.serverBuilder = serverBuilder;
        this.applicationLifetime = applicationLifetime;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await applicationLifetime.WaitForApplicationStartedAsync(stoppingToken).ConfigureAwait(false);

            var server = await serverBuilder.BuildAsync().ConfigureAwait(false);
            await using (server.ConfigureAwait(false))
            {
                LogStarted();
                await server.RunAsync(stoppingToken).ConfigureAwait(false);
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