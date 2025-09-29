#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

namespace Mqtt.Server.Identity.CosmosDB;

public static class PropertyBuilderExtensions
{
    extension(IConventionPropertyBuilder builder)
    {
        public IConventionPropertyBuilder? IsETagConcurrency()
        {
            return builder
                .IsConcurrencyToken(true)?
                .ToJsonProperty("_etag")?
                .ValueGenerated(ValueGenerated.OnAddOrUpdate);
        }
    }
}