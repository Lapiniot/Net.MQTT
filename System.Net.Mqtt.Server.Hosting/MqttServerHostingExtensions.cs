﻿using System.Net.Mqtt.Server.Hosting.Configuration;
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

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder)
        {
            if(hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
            
            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServerOptions>(context.Configuration.GetSection(RootSectionName)));
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, Action<MqttServerOptions> configureOptions)
        {
            if(hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
            if(configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServerOptions>(context.Configuration.GetSection(RootSectionName))
                .PostConfigure(configureOptions));
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, string name)
        {
            if(hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
            if(name == null) throw new ArgumentNullException(nameof(name));

            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServerOptions>(name, context.Configuration.GetSection($"{RootSectionName}:{name}")));
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder, string name, Action<MqttServerOptions> configureOptions)
        {
            if(hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            return hostBuilder.ConfigureServices((context, services) => services
                .Configure<MqttServerOptions>(name, context.Configuration.GetSection($"{RootSectionName}:{name}"))
                .PostConfigure(name, configureOptions));
        }

        public static IHostBuilder UseMqttService(this IHostBuilder hostBuilder)
        {
            if(hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder.ConfigureServices((context, services) => services.AddDefaultMqttServerBuilder().AddMqttService());
        }

        public static IHostBuilder UseMqttService(this IHostBuilder hostBuilder, string name)
        {
            if(hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
            if(name == null) throw new ArgumentNullException(nameof(name));

            return hostBuilder.ConfigureServices((context, services) => services
                .AddMqttService(provider => new MqttServerBuilder(
                    provider.GetService<ILoggerFactory>(),
                    provider.GetService<IOptionsFactory<MqttServerOptions>>().Create(name),
                    null)));
        }

        public static IServiceCollection AddDefaultMqttServerBuilder(this IServiceCollection services)
        {
            if(services == null) throw new ArgumentNullException(nameof(services));

            return services.Replace(ServiceDescriptor.Transient<IMqttServerBuilder, MqttServerBuilder>());
        }

        public static IServiceCollection AddMqttService(this IServiceCollection services)
        {
            if(services == null) throw new ArgumentNullException(nameof(services));

            return services.AddSingleton<IHostedService, MqttService>();
        }

        public static IServiceCollection AddMqttService(this IServiceCollection services, Func<IServiceProvider, IMqttServerBuilder> builderFactory)
        {
            return services.AddSingleton<IHostedService>(provider =>
                new MqttService(provider.GetRequiredService<ILogger<MqttService>>(), builderFactory(provider)));
        }
    }
}