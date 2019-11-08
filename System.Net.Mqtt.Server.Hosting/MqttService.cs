using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttService : BackgroundService, IAsyncDisposable
    {
        private readonly ILogger<MqttService> logger;
        private readonly MqttServer server;

        public MqttService(ILogger<MqttService> logger, IMqttServerBuilder builder)
        {
            this.logger = logger;
            if(builder == null) throw new ArgumentNullException(nameof(builder));
            server = builder.Build();
        }

        public async ValueTask DisposeAsync()
        {
            await server.DisposeAsync().ConfigureAwait(false);
            base.Dispose();
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

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            var _ = server.DisposeAsync();
            base.Dispose();
        }
    }
}