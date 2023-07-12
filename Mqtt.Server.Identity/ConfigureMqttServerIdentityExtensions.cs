using System.Diagnostics.CodeAnalysis;

namespace Mqtt.Server.Identity;

public static class ConfigureMqttServerIdentityExtensions
{
    public static IdentityBuilder AddMqttServerIdentity(this IServiceCollection services) =>
        AddMqttServerIdentity(services, o => { });

    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IdentityBuilder AddMqttServerIdentity(this IServiceCollection services, Action<IdentityOptions> configureOptions)
    {
        if (!services.Any(sd => sd.ServiceType == typeof(IRazorPageActivator)))
        {
            services.AddRazorPages();
        }

        services.AddMvc().ConfigureApplicationPartManager(SetupApplicationParts);

        return services
            .AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>()
            .AddDefaultIdentity<IdentityUser>(configureOptions)
            .AddRoles<IdentityRole>();
    }

    public static IdentityBuilder AddMqttServerIdentityStore(this IdentityBuilder builder, Action<DbContextOptionsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<ApplicationDbContext>(configure);
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        return builder.AddEntityFrameworkStores<ApplicationDbContext>();
    }

    public static IEndpointConventionBuilder MapMqttServerIdentityUI(this IEndpointRouteBuilder builder) => builder.MapRazorPages();

    private static void SetupApplicationParts(ApplicationPartManager apm)
    {
        var factory = new ConsolidatedAssemblyApplicationPartFactory();
        foreach (var part in factory.GetApplicationParts(typeof(ConfigureMqttServerIdentityExtensions).Assembly))
        {
            apm.ApplicationParts.Add(part);
        }
    }
}