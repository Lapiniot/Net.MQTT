﻿using System.Linq;
using System.Net.Listeners;
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

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, Action<MqttServerOptions> configureOptions)
        {
            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServerOptions>(context.Configuration.GetSection(RootSectionName))
                .PostConfigure(configureOptions)
                .AddDefaultMqttServerFactory()
                .AddMqttService());
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, string name, Action<MqttServerOptions> configureOptions)
        {
            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServerOptions>(name, context.Configuration.GetSection($"{RootSectionName}:{name}"))
                .PostConfigure(name, configureOptions)
                .AddMqttService(provider => new MqttServerBuilder(
                    provider.GetService<ILoggerFactory>(),
                    provider.GetService<IOptionsFactory<MqttServerOptions>>().Create(name),
                    provider.GetServices<IConnectionListener>()?.ToArray())));
        }

        public static IServiceCollection AddDefaultMqttServerFactory(this IServiceCollection services)
        {
            return services.Replace(ServiceDescriptor.Singleton<IMqttServerBuilder, MqttServerBuilder>());
        }

        public static IServiceCollection AddMqttService(this IServiceCollection services)
        {
            return services.AddSingleton<IHostedService, MqttService>();
        }

        public static IServiceCollection AddMqttService(this IServiceCollection services, Func<IServiceProvider, IMqttServerBuilder> serverFactory)
        {
            return services.AddSingleton<IHostedService>(provider =>
                new MqttService(provider.GetService<ILogger<MqttService>>(), serverFactory(provider)));
        }
    }
}