using System.Diagnostics.CodeAnalysis;

namespace Mqtt.Server.Web;

public static class ConfigureMqttServerUIExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UIOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IServiceCollection AddMqttServerUI(this IServiceCollection services, Action<UIOptions>? configureOptions = null)
    {
        services.AddOptions<UIOptions>().BindConfiguration("AdminWebUI").Configure(configureOptions ?? (static _ => { }));

        if (!services.Any(sd => sd.ServiceType == typeof(IRazorPageActivator)))
        {
            services.AddRazorPages();
        }

        services.AddServerSideBlazor();

        services.AddMvc().ConfigureApplicationPartManager(apm =>
            {
                var factory = new ConsolidatedAssemblyApplicationPartFactory();
                foreach (var part in factory.GetApplicationParts(typeof(ConfigureMqttServerUIExtensions).Assembly))
                {
                    apm.ApplicationParts.Add(part);
                }
            });

        return services;
    }

    public static IEndpointConventionBuilder MapMqttServerUI(this IEndpointRouteBuilder builder)
    {
        builder.MapBlazorHub();
        return builder.MapFallbackToPage("/_Host");
    }
}