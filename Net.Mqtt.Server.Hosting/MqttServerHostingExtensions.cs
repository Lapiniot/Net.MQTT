using System.Diagnostics.CodeAnalysis;
using Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Net.Mqtt.Server.Hosting;

public static class MqttServerHostingExtensions
{
    public static IServiceCollection AddMqttServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<MqttServerOptions>();
        services.TryAddTransient<IValidateOptions<MqttServerOptions>, ServerOptionsValidator>();
        services.TryAddTransient<IMqttServerBuilder, MqttServerBuilder>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IMqttServerBuilder>().Build());
        services.AddHostedService<GenericMqttHostService>();
        return services;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttServerOptions))]
    public static IHostBuilder ConfigureMqttServer(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, MqttServerOptionsBuilder> configureOptions = null,
        string configSectionPath = "MQTT")
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentException.ThrowIfNullOrEmpty(configSectionPath);

        return hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.AddTransient<IConfigureOptions<MqttServerOptions>, MqttServerOptionsSetup>(sp => new(ctx.Configuration.GetSection(configSectionPath)));
            configureOptions?.Invoke(ctx, new(services.AddOptions<MqttServerOptions>()));
        });
    }

    public static IServiceCollection AddMqttAuthentication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IMqttAuthenticationHandler
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMqttAuthenticationHandler, T>();
        return services;
    }

    public static IServiceCollection AddMqttAuthentication(this IServiceCollection services, Func<string, string, bool> callback)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(callback);

        services.TryAddSingleton<IMqttAuthenticationHandler>(sp => new CallbackAuthenticationHandler(callback));
        return services;
    }

    public static IServiceCollection AddMqttCertificateValidation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
        (this IServiceCollection services) where T : class, IRemoteCertificateValidationPolicy
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IRemoteCertificateValidationPolicy, T>();
        return services;
    }

    private sealed class CallbackAuthenticationHandler(Func<string, string, bool> callback) : IMqttAuthenticationHandler
    {
        public bool Authenticate(string userName, string password) => callback(userName, password);
    }
}