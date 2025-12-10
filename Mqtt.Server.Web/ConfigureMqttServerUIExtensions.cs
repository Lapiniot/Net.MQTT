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

namespace Mqtt.Server.Web;

public static class ConfigureMqttServerUIExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UIOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CircuitOptions))]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static IServiceCollection AddMqttServerUI(this IServiceCollection services, string? configSectionPath = null, Action<UIOptions>? configureOptions = null)
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

    public static IServiceCollection AddIdentitySmtpEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<SmtpSenderOptions, SmtpSenderOptionsValidator>()
            .Bind(configuration);
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        return services;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static RazorComponentsEndpointConventionBuilder MapMqttServerUI(this IEndpointRouteBuilder builder)
    {
        builder.MapAdditionalIdentityEndpoints();
        return builder.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MetricsCollectorOptions))]
    public static IServiceCollection AddMqttServerMetricsCollector(this IServiceCollection services)
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