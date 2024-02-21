using System.Diagnostics.CodeAnalysis;
using Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Net.Mqtt.Server.Hosting;

public static class MqttServerHostingExtensions
{
    public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.TryAddTransient<IMqttServerBuilder, MqttServerBuilder>();
            services.AddSingleton(sp => sp.GetRequiredService<IMqttServerBuilder>().Build());
            services.AddHostedService<GenericMqttHostService>();
        });
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServerOptions))]
    public static IHostBuilder ConfigureMqttServerOptions(this IHostBuilder hostBuilder, string configSectionPath = "MQTT")
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentException.ThrowIfNullOrEmpty(configSectionPath);

        return hostBuilder.ConfigureServices((ctx, services) => services
            .AddTransient<IConfigureOptions<ServerOptions>, ServerOptionsConfigurator>(sp =>
                new(ctx.Configuration.GetSection(configSectionPath)))
            .AddTransient<IValidateOptions<ServerOptions>, ServerOptionsValidator>()
            .AddOptions<ServerOptions>());
    }

    public static IHostBuilder AddMqttAuthentication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IHostBuilder builder) where T : class, IMqttAuthenticationHandler
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<IMqttAuthenticationHandler, T>());
    }

    public static IHostBuilder AddMqttAuthentication(this IHostBuilder builder, Func<string, string, bool> callback)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.ConfigureServices((_, services) =>
            services.AddTransient<IMqttAuthenticationHandler>(sp =>
                new CallbackAuthenticationHandler(callback)));
    }

    public static IHostBuilder AddMqttCertificateValidation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IHostBuilder builder) where T : class, ICertificateValidationPolicy
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<ICertificateValidationPolicy, T>());
    }

    private sealed class CallbackAuthenticationHandler(Func<string, string, bool> callback) : IMqttAuthenticationHandler
    {
        public bool Authenticate(string userName, string password) => callback(userName, password);
    }
}