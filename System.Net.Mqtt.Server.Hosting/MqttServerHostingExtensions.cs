using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting
{
    public static class MqttServerHostingExtensions
    {
        private const string RootSectionName = "MQTT";

        public static IServiceCollection ConfigureMqttService(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<MqttServerBuilderOptions>(configuration);
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder)
        {
            if(hostBuilder is null) throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder.ConfigureServices((context, services) =>
                services.ConfigureMqttService(context.Configuration.GetSection(RootSectionName)));
        }

        public static IHostBuilder UseMqttService(this IHostBuilder hostBuilder)
        {
            if(hostBuilder is null) throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder.ConfigureServices((context, services) =>
                services.AddDefaultMqttServerBuilder().AddMqttService());
        }

        public static IServiceCollection AddDefaultMqttServerBuilder(this IServiceCollection services)
        {
            return services.Replace(ServiceDescriptor.Transient<IMqttServerBuilder, MqttServerBuilder>());
        }

        public static IServiceCollection AddMqttService(this IServiceCollection services)
        {
            return services.AddHostedService<GenericMqttHostService>();
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