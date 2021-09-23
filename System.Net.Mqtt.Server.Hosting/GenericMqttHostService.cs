using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

public sealed partial class GenericMqttHostService : BackgroundService, IAsyncDisposable
{
    private readonly IMqttServer server;

    public GenericMqttHostService(IMqttServerBuilder serverBuilder, ILogger<GenericMqttHostService> logger)
    {
        ArgumentNullException.ThrowIfNull(serverBuilder);
        ArgumentNullException.ThrowIfNull(logger);

        server = serverBuilder.Build();
        this.logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        await using(server)
        {
            Dispose();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await server.RunAsync(stoppingToken).ConfigureAwait(false);
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