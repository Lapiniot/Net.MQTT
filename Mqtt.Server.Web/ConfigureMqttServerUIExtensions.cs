namespace Mqtt.Server.Web;

public static class ConfigureMqttServerUIExtensions
{
    public static IServiceCollection AddMqttServerUI(this IServiceCollection services)
    {
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