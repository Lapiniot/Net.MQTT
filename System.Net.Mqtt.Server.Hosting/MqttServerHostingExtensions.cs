using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting;

public static class MqttServerHostingExtensions
{
    public static IHostBuilder ConfigureMqttHost(this IHostBuilder hostBuilder, Action<IMqttHostBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(new MqttHostBuilder(hostBuilder));

        return hostBuilder;
    }

    public static IMqttHostBuilder AddAuthentication<T>(this IMqttHostBuilder builder)
        where T : class, IMqttAuthenticationHandler
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<IMqttAuthenticationHandler, T>());
    }

    public static IMqttHostBuilder AddAuthentication(this IMqttHostBuilder builder, Func<IServiceProvider, IMqttAuthenticationHandler> implementationFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient(implementationFactory));
    }

    public static IMqttHostBuilder AddCertificateValidation<T>(this IMqttHostBuilder builder)
        where T : class, ICertificateValidationPolicy
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<ICertificateValidationPolicy, T>());
    }
}