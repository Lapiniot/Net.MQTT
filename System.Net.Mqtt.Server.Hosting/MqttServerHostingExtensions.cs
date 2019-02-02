using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public static class MqttServerHostingExtensions
    {
        private const string RootSectionName = "MQTT";

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, Action<MqttServiceOptions> configureOptions)
        {
            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServiceOptions>(context.Configuration.GetSection(RootSectionName))
                .PostConfigure(configureOptions)
                .AddDefaultMqttServerFactory()
                .AddSingleton<IHostedService, MqttService>());
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, string name, Action<MqttServiceOptions> configureOptions)
        {
            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServiceOptions>(name, context.Configuration.GetSection($"{RootSectionName}:{name}"))
                .PostConfigure(name, configureOptions)
                .AddSingleton<IHostedService>(provider =>
                    new MqttService(provider.GetService<ILogger<MqttService>>(),
                        new DefaultMqttServerFactory(
                            provider.GetService<ILogger<DefaultMqttServerFactory>>(),
                            provider.GetService<IOptionsFactory<MqttServiceOptions>>().Create(name)))));
        }

        public static IServiceCollection AddDefaultMqttServerFactory(this IServiceCollection services)
        {
            return services.Replace(ServiceDescriptor.Singleton<IMqttServerFactory, DefaultMqttServerFactory>());
        }
    }
}