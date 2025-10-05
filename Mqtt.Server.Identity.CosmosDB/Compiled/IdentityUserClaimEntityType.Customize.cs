namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class IdentityUserClaimEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(IdentityUserClaim<>.UserId)]);
    }
}