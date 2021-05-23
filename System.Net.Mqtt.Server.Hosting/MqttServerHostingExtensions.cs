using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting
{
    public static class MqttServerHostingExtensions
    {
        public static IHostBuilder ConfigureMqttHost(this IHostBuilder hostBuilder, Action<IMqttHostBuilder> configure)
        {
            if(configure is null) throw new ArgumentNullException(nameof(configure));

            configure(new MqttHostBuilder(hostBuilder));

            return hostBuilder;
        }

        public static IMqttHostBuilder UseAuthentication<T>(this IMqttHostBuilder builder)
            where T : class, IMqttAuthenticationHandler
        {
            if(builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureServices((ctx, services) => services.AddTransient<IMqttAuthenticationHandler, T>());
        }

        public static IMqttHostBuilder UseAuthentication(this IMqttHostBuilder builder, Func<IServiceProvider, IMqttAuthenticationHandler> implementationFactory)
        {
            if(builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureServices((ctx, services) => services.AddTransient<IMqttAuthenticationHandler>(implementationFactory));
        }
    }
}