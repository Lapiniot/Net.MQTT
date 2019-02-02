using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttService : BackgroundService
    {
        private readonly IMqttServerFactory factory;
        private readonly ILogger<MqttService> logger;
        private MqttServer server;

        public MqttService(ILogger<MqttService> logger, IMqttServerFactory factory)
        {
            this.logger = logger;
            this.factory = factory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return (server = factory.Create()).RunAsync(stoppingToken);
        }

        public override void Dispose()
        {
            base.Dispose();
            _ = server.DisposeAsync();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting MQTT service...");
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Started MQTT service.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping MQTT service...");
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Stopped MQTT service.");
        }
    }
}