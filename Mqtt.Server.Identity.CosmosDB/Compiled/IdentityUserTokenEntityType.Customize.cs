namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class IdentityUserTokenEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(IdentityUserToken<>.UserId)]);
    }
}