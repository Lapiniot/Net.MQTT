using System.Diagnostics.CodeAnalysis;
using Mqtt.Server.Web.Components;

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

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return services;
    }

    public static IEndpointConventionBuilder MapMqttServerUI(this IEndpointRouteBuilder builder) => builder
        .MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
}