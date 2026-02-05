using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Mqtt.Server.Identity.Data;
using Mqtt.Server.Web.Components;
using OOs.Extensions.Diagnostics;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1708 // Identifiers should differ by more than case

namespace Mqtt.Server.Web;

public static class ConfigureMqttServerUIExtensions
{
    extension(IServiceCollection services)
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UIOptions))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CircuitOptions))]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public IServiceCollection AddMqttServerUI(string? configSectionPath = null, Action<UIOptions>? configureOptions = null)
        {
            var builder = services.AddOptions<UIOptions>().BindConfiguration(configSectionPath ?? "AdminWebUI");

            if (configureOptions is not null)
            {
                builder.Configure(configureOptions);
            }

            services.TryAddTransient<IEmailSender, NoOpEmailSender>();
            services.TryAddTransient<IEmailSender<ApplicationUser>, DefaultIdentityEmailSender>();

            services.AddScoped<IdentityRedirectManager>();
            services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
            services.AddCascadingAuthenticationState();

            services.AddRazorComponents().AddInteractiveServerComponents();

            services.AddAuthorizationBuilder().AddPolicy("manage-connections", builder => builder.RequireClaim(ClaimTypes.Role, "Admin"));

            services.AddMqttServerMetricsCollector();

            return services;
        }

        public IServiceCollection AddIdentitySmtpEmailSender(IConfiguration configuration)
        {
            services.AddOptionsWithValidateOnStart<SmtpSenderOptions, SmtpSenderOptionsValidator>()
                .Bind(configuration);
            services.AddTransient<IEmailSender, SmtpEmailSender>();
            return services;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MetricsCollectorOptions))]
        public IServiceCollection AddMqttServerMetricsCollector()
        {
            services.AddOptions<MetricsCollectorOptions>(MqttServerMetricsCollector.OptionsName)
                .BindConfiguration($"MetricsCollector:{MqttServerMetricsCollector.OptionsName}");

            services.TryAddSingleton<IOptionsChangeTokenSource<MetricsCollectorOptions>>(
                sp => new ConfigurationChangeTokenSource<MetricsCollectorOptions>(
                    MqttServerMetricsCollector.OptionsName, sp.GetRequiredService<IConfiguration>()));

            services.TryAddSingleton<MqttServerMetricsCollector>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricsListener, MqttServerMetricsCollector>(
                static sp => sp.GetRequiredService<MqttServerMetricsCollector>()));

            return services;
        }
    }

    extension(IEndpointRouteBuilder builder)
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public RazorComponentsEndpointConventionBuilder MapMqttServerUI()
        {
            builder.MapAdditionalIdentityEndpoints();
            return builder.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        }
    }
}