namespace Mqtt.Server.Identity;

public static class ConfigureMqttServerIdentityExtensions
{
    public static IdentityBuilder AddMqttServerIdentity(this IServiceCollection services) =>
        AddMqttServerIdentity(services, o => { });

    public static IdentityBuilder AddMqttServerIdentity(this IServiceCollection services, Action<IdentityOptions> configureOptions)
    {
        services.AddRazorPages();
        services.AddMvc().ConfigureApplicationPartManager(SetupApplicationParts);

        return services
            .AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>()
            .AddDefaultIdentity<IdentityUser>(configureOptions)
            .AddRoles<IdentityRole>();
    }

    public static IdentityBuilder AddMqttServerIdentityStore(this IdentityBuilder builder, Action<DbContextOptionsBuilder>? configure = null)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(configure);
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        return builder.AddEntityFrameworkStores<ApplicationDbContext>();
    }

    private static void SetupApplicationParts(ApplicationPartManager apm)
    {
        var factory = new ConsolidatedAssemblyApplicationPartFactory();
        foreach (var part in factory.GetApplicationParts(typeof(ConfigureMqttServerIdentityExtensions).Assembly))
        {
            apm.ApplicationParts.Add(part);
        }
    }
}