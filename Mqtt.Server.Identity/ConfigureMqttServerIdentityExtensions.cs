using Microsoft.Extensions.DependencyInjection;
using Net.Mqtt.Server;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1708 // Identifiers should differ by more than case

namespace Mqtt.Server.Identity;

public static class ConfigureMqttServerIdentityExtensions
{
    extension(IServiceCollection services)
    {
        public IdentityBuilder AddMqttServerIdentity() => AddMqttServerIdentity(services, o => { });

        public IdentityBuilder AddMqttServerIdentity(Action<IdentityOptions> configureOptions)
        {
            return services
                .AddIdentityCore<ApplicationUser>(configureOptions)
                .AddRoles<IdentityRole>()
                .AddSignInManager()
                .AddDefaultTokenProviders();
        }

        public IServiceCollection AddMqttAuthenticationWithIdentity()
        {
            services.AddTransient<IMqttAuthenticationHandler, IdentityMqttAuthenticationHandler>();
            return services;
        }
    }

    extension(IdentityBuilder builder)
    {
        public IdentityBuilder AddMqttServerIdentityStores(Action<DbContextOptionsBuilder> optionsAction)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.AddDbContext<ApplicationDbContext>(optionsAction);
            return builder.AddEntityFrameworkStores<ApplicationDbContext>();
        }
    }
}