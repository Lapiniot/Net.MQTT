using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttService : BackgroundService
    {
        private readonly ILogger<MqttService> logger;
        private readonly MqttServer server;

        public MqttService(ILogger<MqttService> logger, IMqttServerFactory factory)
        {
            this.logger = logger;
            server = factory.Create();
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

        public override void Dispose()
        {
            base.Dispose();
            _ = server.DisposeAsync();
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