namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class IdentityRoleClaimEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(IdentityRoleClaim<>.RoleId)]);
    }
}