using Microsoft.Extensions.DependencyInjection;

namespace Mqtt.Server.Identity;

public static class ConfigureMqttServerIdentityExtensions
{
    public static IdentityBuilder AddMqttServerIdentity(this IServiceCollection services) =>
        AddMqttServerIdentity(services, o => { });

    public static IdentityBuilder AddMqttServerIdentity(this IServiceCollection services, Action<IdentityOptions> configureOptions)
    {
        return services
            .AddIdentityCore<ApplicationUser>(configureOptions)
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }

    public static IdentityBuilder AddMqttServerIdentityStore(this IdentityBuilder builder, Action<DbContextOptionsBuilder> optionsAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddDbContext<ApplicationDbContext>(optionsAction);
        return builder.AddEntityFrameworkStores<ApplicationDbContext>();
    }
}