using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting
{
    public sealed class GenericMqttHostService : BackgroundService, IAsyncDisposable
    {
        private readonly IMqttServer server;
        private readonly ILogger<GenericMqttHostService> logger;

        public GenericMqttHostService(IMqttServer server, ILogger<GenericMqttHostService> logger)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask DisposeAsync()
        {
            base.Dispose();
            await server.DisposeAsync().ConfigureAwait(false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await server.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch(Exception exception)
            {
                logger.LogError(exception, exception.Message);
                throw;
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting hosted MQTT service...");
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Started hosted MQTT service.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping hosted MQTT service...");
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Stopped hosted MQTT service.");
        }
    }
}