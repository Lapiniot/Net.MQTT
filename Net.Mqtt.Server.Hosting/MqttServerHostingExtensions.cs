using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Net.Mqtt.Server.Hosting;

public static class MqttServerHostingExtensions
{
    public static IServiceCollection AddMqttServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptionsWithValidateOnStart<MqttServerOptions, MqttServerOptionsValidator>();
        services.TryAddTransient<IMqttServerBuilder, MqttServerBuilder>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IMqttServerBuilder>().Build());
        services.AddHostedService<GenericMqttHostService>();
        return services;
    }

    [DynamicDependency(All, typeof(MqttServerOptions))]
    [DynamicDependency(All, typeof(MqttOptions))]
    [DynamicDependency(All, typeof(MqttOptions5))]
    [DynamicDependency(All, typeof(MqttEndpoint))]
    [DynamicDependency(All, typeof(CertificateOptions))]
    [UnconditionalSuppressMessage("Trimming", "IL3050:Members annotated with 'RequiresUnreferencedCodeAttribute'" +
        " require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute'" +
        " require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    public static IHostBuilder ConfigureMqttServer(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, MqttServerOptionsBuilder> configureOptions = null,
        string configSectionPath = "MQTT")
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentException.ThrowIfNullOrEmpty(configSectionPath);

        return hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.AddTransient<IConfigureOptions<MqttServerOptions>, MqttServerOptionsSetup>(
                sp => new(ctx.Configuration.GetSection(configSectionPath)));
            configureOptions?.Invoke(ctx, new(services.AddOptions<MqttServerOptions>()));
        });
    }

    public static IServiceCollection AddMqttAuthentication<[DynamicallyAccessedMembers(All)] T>
        (this IServiceCollection services) where T : class, IMqttAuthenticationHandler
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMqttAuthenticationHandler, T>();
        return services;
    }

    public static IServiceCollection AddMqttAuthentication(this IServiceCollection services,
        Func<string, string, bool> callback)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(callback);

        services.TryAddSingleton<IMqttAuthenticationHandler>(sp => new CallbackAuthenticationHandler(callback));
        return services;
    }

    public static IServiceCollection AddMqttCertificateValidation<[DynamicallyAccessedMembers(All)] T>
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