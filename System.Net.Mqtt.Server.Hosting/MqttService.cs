using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttService : BackgroundService
    {
        private MqttServer server;

        public MqttService(ILogger<MqttService> logger, IOptions<MqttServiceOptions> options)
        {
            Logger = logger;
            Options = options;
        }

        public ILogger Logger { get; }
        public IOptions<MqttServiceOptions> Options { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var setup = Options.Value;

            server = new MqttServer();
            foreach(var (name, listener) in setup.Listeners)
            {
                server.RegisterListener(name, listener);
            }

            return server.RunAsync(stoppingToken);
        }

        public override void Dispose()
        {
            base.Dispose();
            var unused = server.DisposeAsync();
        }
    }
}