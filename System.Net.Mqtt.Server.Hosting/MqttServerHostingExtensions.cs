using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting
{
    public static class MqttServerHostingExtensions
    {
        public static IHostBuilder ConfigureMqttServer(this IHostBuilder hostBuilder, Action<MqttServiceOptions> configureOptions)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var config = context.Configuration.GetSection("MQTT");
                if(config != null) services.Configure<MqttServiceOptions>(config);
                services.Configure(configureOptions);
                services.AddTransient<IHostedService, MqttService>();
            });
            return hostBuilder;
        }
    }
}