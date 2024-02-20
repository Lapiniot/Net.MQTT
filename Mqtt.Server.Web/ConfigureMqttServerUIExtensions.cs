using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Diagnostics.Metrics;
using Mqtt.Server.Web.Components;
using OOs.Extensions.Diagnostics;

namespace Mqtt.Server.Web;

public static class ConfigureMqttServerUIExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UIOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IServiceCollection AddMqttServerUI(this IServiceCollection services, string? configSectionPath = null, Action<UIOptions>? configureOptions = null)
    {
        var builder = services.AddOptions<UIOptions>().BindConfiguration(configSectionPath ?? "AdminWebUI");

        if (configureOptions is not null)
        {
            builder.Configure(configureOptions);
        }

        services.AddScoped<UserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();

        services.AddRazorComponents().AddInteractiveServerComponents();

        services.AddAuthorizationBuilder().AddPolicy("manage-connections", builder => builder.RequireClaim(ClaimTypes.Role, "Admin"));

        services.AddOptions<MetricsCollectorOptions>().BindConfiguration("MetricsCollector:MqttServer");
        services.AddMetrics(builder => builder.AddListener<MqttServerMetricsCollector>());

        return services;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static RazorComponentsEndpointConventionBuilder MapMqttServerUI(this IEndpointRouteBuilder builder)
    {
        builder.MapAdditionalIdentityEndpoints();
        return builder
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }
}