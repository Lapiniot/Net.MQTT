using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting;

public static class MqttServerHostingExtensions
{
    private const string RootSectionName = "MQTT";

    public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, services) => services
            .AddTransient<IMqttServerBuilder, MqttServerBuilder>()
            .AddHostedService<GenericMqttHostService>()
            .AddOptions<MqttServerBuilderOptions>()
            .ValidateDataAnnotations());
    }

    public static IHostBuilder ConfigureMqttServerDefaults(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder
            .ConfigureAppConfiguration((_, cb) => cb.AddEnvironmentVariables($"{RootSectionName}_"))
            .ConfigureServices((_, services) =>
            {
                services.AddOptions<MqttServerBuilderOptions>().Configure(options =>
                {
                    options.ConnectTimeout = 5000;
                    options.DisconnectTimeout = 10000;
                    options.MaxInFlight = short.MaxValue;
                    options.ProtocolLevel = ProtocolLevel.All;
                }).BindConfiguration(RootSectionName);
                services.AddTransient<IConfigureOptions<MqttServerBuilderOptions>, MqttServerBuilderOptionsEndpointsConfigurator>();
            });
    }

    public static IHostBuilder ConfigureMqttServerBuilderOptions(this IHostBuilder hostBuilder, Action<OptionsBuilder<MqttServerBuilderOptions>> configure)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configure);

        return hostBuilder.ConfigureServices((_, services) => configure(services.AddOptions<MqttServerBuilderOptions>()));
    }

    public static IHostBuilder AddMqttAuthentication<T>(this IHostBuilder builder) where T : class, IMqttAuthenticationHandler
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<IMqttAuthenticationHandler, T>());
    }

    public static IHostBuilder AddMqttAuthentication(this IHostBuilder builder, Func<IServiceProvider, IMqttAuthenticationHandler> implementationFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient(implementationFactory));
    }

    public static IHostBuilder AddMqttCertificateValidation<T>(this IHostBuilder builder) where T : class, ICertificateValidationPolicy
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<ICertificateValidationPolicy, T>());
    }
}