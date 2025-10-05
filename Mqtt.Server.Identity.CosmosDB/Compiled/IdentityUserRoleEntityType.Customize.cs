namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class IdentityUserRoleEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(IdentityUserRole<>.RoleId)]);
    }
}