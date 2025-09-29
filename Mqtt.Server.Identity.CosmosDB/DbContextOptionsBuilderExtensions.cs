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
                .WithConvention<IdentityModelConfigurationConvention>();
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static CosmosDbContextOptionsBuilder Configure(this CosmosDbContextOptionsBuilder builder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConnectionMode(configuration.GetValue("ConnectionMode", ConnectionMode.Direct)).LimitToEndpoint();
    }
}