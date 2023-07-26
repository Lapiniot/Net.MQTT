using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting;

public static class MqttServerHostingExtensions
{
    private const string RootSectionName = "MQTT";

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServerOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.TryAddTransient<IMqttServerBuilder, MqttServerBuilder>();
            services.AddOptions<ServerOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton(sp => sp.GetRequiredService<IMqttServerBuilder>().Build());
            services.AddHostedService<GenericMqttHostService>();
        });
    }

    public static IHostBuilder ConfigureMqttServerDefaults(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder
            .ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddEnvironmentVariables($"{RootSectionName}_"))
            .ConfigureServices((_, services) => services.AddTransient<IConfigureOptions<ServerOptions>, ServerOptionsConfigurator>());
    }

    public static IHostBuilder ConfigureMqttServerBuilderOptions(this IHostBuilder hostBuilder, Action<OptionsBuilder<ServerOptions>> configure)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configure);

        return hostBuilder.ConfigureServices((_, services) => configure(services.AddOptions<ServerOptions>()));
    }

    public static IHostBuilder AddMqttAuthentication<T>(this IHostBuilder builder) where T : class, IMqttAuthenticationHandler
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

    public static IHostBuilder AddMqttCertificateValidation<T>(this IHostBuilder builder) where T : class, ICertificateValidationPolicy
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddTransient<ICertificateValidationPolicy, T>());
    }

    private sealed class CallbackAuthenticationHandler : IMqttAuthenticationHandler
    {
        private readonly Func<string, string, bool> callback;

        public CallbackAuthenticationHandler(Func<string, string, bool> callback) => this.callback = callback;

        public bool Authenticate(string userName, string password) => callback(userName, password);
    }
}