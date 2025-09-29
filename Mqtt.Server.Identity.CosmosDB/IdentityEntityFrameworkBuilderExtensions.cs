namespace Mqtt.Server.Identity.CosmosDB;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

public static class IdentityEntityFrameworkBuilderExtensions
{
    extension(IdentityBuilder builder)
    {
        public IdentityBuilder AddCosmosIdentityStores() =>
            builder.AddUserStore<CosmosUserStore>();
    }
}