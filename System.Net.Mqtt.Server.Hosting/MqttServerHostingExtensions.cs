using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting
{
    public static class MqttServerHostingExtensions
    {
        private const string RootSectionName = "MQTT";


        public static IServiceCollection ConfigureMqttServerOptions(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .Configure<MqttServerBuilderOptions>(configuration)
                .PostConfigure<MqttServerBuilderOptions>(options =>
                {
                    foreach(var item in configuration.GetSection("Endpoints").GetChildren())
                    {
                        options.UseEndpoint(item.Key, new Uri(item.Value ?? item.GetValue<string>("Url")));
                    }
                });
        }

        public static IServiceCollection ConfigureMqttServerOptions(this IServiceCollection services,
            Action<MqttServerBuilderOptions> configureOptions)
        {
            return services.PostConfigure<MqttServerBuilderOptions>(configureOptions);
        }

        public static IHostBuilder ConfigureMqttServer(this IHostBuilder hostBuilder)
        {
            return ConfigureMqttServer(hostBuilder, (_, __) => { });
        }

        public static IHostBuilder ConfigureMqttServer(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection> configure)
        {
            if(hostBuilder is null) throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder
                .ConfigureServices((context, services) => services
                    .ConfigureMqttServerOptions(context.Configuration.GetSection(RootSectionName))
                    .AddTransient<IMqttServerBuilder, MqttServerBuilder>()
                    .AddHostedService<GenericMqttHostService>())
                .ConfigureServices(configure);
        }

        public static IServiceCollection AddMqttAuthentication<T>(this IServiceCollection services)
            where T : class, IMqttAuthenticationHandler
        {
            return services.AddTransient<IMqttAuthenticationHandler, T>();
        }

        public static IServiceCollection AddMqttAuthentication(this IServiceCollection services,
            Func<IServiceProvider, IMqttAuthenticationHandler> implementationFactory)
        {
            return services.AddTransient<IMqttAuthenticationHandler>(implementationFactory);
        }
    }
}