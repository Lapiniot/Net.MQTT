using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Mqtt.Server.Identity.CosmosDB;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

public static class DbContextOptionsBuilderExtensions
{
    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder ConfigureCosmos(string connectionString, string databaseName,
            Action<CosmosDbContextOptionsBuilder>? cosmosOptionsAction = null)
        {
            return builder
                .UseCosmos(connectionString, databaseName, cosmosOptionsAction)
                .UseAsyncSeeding(SeedAsync)
                .WithConvention<IdentityModelConfigurationConvention>();
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static CosmosDbContextOptionsBuilder Configure(this CosmosDbContextOptionsBuilder builder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConnectionMode(configuration.GetValue("ConnectionMode", ConnectionMode.Direct)).LimitToEndpoint();
    }

    private static async Task SeedAsync(DbContext ctx, bool _, CancellationToken token)
    {
        var context = (ApplicationDbContext)ctx;

        var adminRole = await context.Roles
            .Where(r => r.NormalizedName == "ADMIN")
            .SingleOrDefaultAsync(token).ConfigureAwait(false);

        if (adminRole is null)
        {
            adminRole = new()
            {
                Name = "Admin",
                NormalizedName = "ADMIN"
            };

            context.Roles.Add(adminRole);

            var adminUser = await context.Users
                .Where(u => u.NormalizedUserName == "ADMIN")
                .SingleOrDefaultAsync(cancellationToken: token).ConfigureAwait(false);

            if (adminUser is null)
            {
                adminUser = new()
                {
                    UserName = "Admin",
                    NormalizedUserName = "ADMIN",
                    Email = "",
                    NormalizedEmail = "",
                    EmailConfirmed = true,
                    PasswordHash = "AQAAAAIAAYagAAAAEEQdyDpdd6xTzS+wuQlIDjhQvIraquzo/G4FTTEkGxV8LaVE0VF4h71K4uNSX5vP5g==",
                    SecurityStamp = "QOJ5ZYOX4VSVUR3AFLWYJ76MEAGE6X5P",
                    LockoutEnabled = true
                };

                context.Users.Add(adminUser);
                context.UserRoles.Add(new() { RoleId = adminRole.Id, UserId = adminUser.Id });
            }
        }

        var clientRole = await context.Roles
            .Where(r => r.NormalizedName == "CLIENT")
            .SingleOrDefaultAsync(token).ConfigureAwait(false);

        if (clientRole is null)
        {
            clientRole = new()
            {
                Name = "Client",
                NormalizedName = "CLIENT"
            };

            context.Roles.Add(clientRole);
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }
}