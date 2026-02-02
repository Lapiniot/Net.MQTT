using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1708 // Identifiers should differ by more than case

namespace Net.Mqtt.Server.Hosting;

/// <summary>
/// Provides extension methods for registering MQTT server services, authentication handlers, and certificate validation policies
/// with the <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// These methods add and configure MQTT server-related services, including options validation, authentication, and certificate validation,
/// to the dependency injection container.
/// </remarks>
public static class MqttServerHostingExtensions
{
    private const string DefaultSectionName = "MQTT";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds required MQTT server services to the <see cref="IServiceCollection"/>
        /// to run the MQTT server as hosted background service.
        /// </summary>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> so that multiple calls can be chained.
        /// </returns>
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
        public IServiceCollection AddMqttServer()
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddOptionsWithValidateOnStart<MqttServerOptions, MqttServerOptionsValidator>();
            services.AddTransient<IConfigureOptions<MqttServerOptions>>(
                sp => new MqttServerOptionsSetup(sp.GetRequiredService<IConfiguration>().GetSection(DefaultSectionName)));
            services.PostConfigure<MqttServerOptions>(options =>
            {
                // Ensure that at least one endpoint is configured
                if (options.Endpoints.Count == 0)
                {
                    options.Endpoints.Add("mqtt", new(new IPEndPoint(IPAddress.Any, 1883)));
                }
            });
            services.TryAddTransient<IMqttServerBuilder, MqttServerBuilder>();
            services.TryAddSingleton(static sp => sp.GetRequiredService<IMqttServerBuilder>().Build());
            services.AddHostedService<GenericMqttHostService>();
            return services;
        }

        /// <summary>
        /// Registers a custom MQTT authentication handler implementation with the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the authentication handler to register.</typeparam>
        /// <returns>The same <see cref="IServiceCollection"/> so that multiple calls can be chained.</returns>
        public IServiceCollection AddMqttAuthentication<[DynamicallyAccessedMembers(All)] T>()
            where T : class, IMqttAuthenticationHandler
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IMqttAuthenticationHandler, T>();
            return services;
        }

        /// <summary>
        /// Registers a callback-based MQTT authentication handler with the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="callback">The callback function to handle authentication requests.</param>
        /// <returns>The same <see cref="IServiceCollection"/> so that multiple calls can be chained.</returns>
        public IServiceCollection AddMqttAuthentication(Func<string, string, ValueTask<bool>> callback)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(callback);

            services.TryAddSingleton<IMqttAuthenticationHandler>(sp => new CallbackAuthenticationHandler(callback));
            return services;
        }

        /// <summary>
        /// Registers a custom remote certificate validation policy implementation with the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the certificate validation policy to register.</typeparam>
        /// <returns>The same <see cref="IServiceCollection"/> so that multiple calls can be chained.</returns>
        public IServiceCollection AddMqttCertificateValidation<[DynamicallyAccessedMembers(All)] T>
            () where T : class, IRemoteCertificateValidationPolicy
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IRemoteCertificateValidationPolicy, T>();
            return services;
        }
    }

    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        /// Configures MQTT server options for the hosted MQTT server instance.
        /// </summary>
        /// <param name="configureOptions">An optional action to configure the MQTT server options.</param>
        /// <param name="configuration">An optional configuration instance to bind MQTT server options from.</param>
        /// <returns>The same <see cref="IHostBuilder"/> so that multiple calls can be chained.</returns>
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
        public IHostBuilder ConfigureMqttServer(Action<HostBuilderContext,
            MqttServerOptionsBuilder> configureOptions = null,
            IConfiguration configuration = null)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);

            return hostBuilder.ConfigureServices((ctx, services) =>
            {
                if (configuration is not null)
                {
                    services.AddTransient<IConfigureOptions<MqttServerOptions>>(
                        sp => new MqttServerOptionsSetup(configuration));
                }

                configureOptions?.Invoke(ctx, new(services.AddOptions<MqttServerOptions>()));
            });
        }
    }

    private sealed class CallbackAuthenticationHandler(Func<string, string, ValueTask<bool>> callback) :
        IMqttAuthenticationHandler
    {
        public ValueTask<bool> AuthenticateAsync(string userName, string password) =>
            callback(userName, password);
    }
}