using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

public sealed partial class GenericMqttHostService : BackgroundService
{
    private readonly IMqttServerBuilder serverBuilder;

    public GenericMqttHostService(IMqttServerBuilder serverBuilder, ILogger<GenericMqttHostService> logger)
    {
        ArgumentNullException.ThrowIfNull(serverBuilder);
        ArgumentNullException.ThrowIfNull(logger);

        this.serverBuilder = serverBuilder;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var server = serverBuilder.Build();
            await using(server.ConfigureAwait(false))
            {
                await server.RunAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch(Exception exception)
        {
            LogError(exception);
            throw;
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        LogStarting();
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
        LogStarted();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        LogStopping();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        LogStopped();
    }
}