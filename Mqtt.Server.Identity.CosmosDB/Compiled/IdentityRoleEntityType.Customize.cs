namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class IdentityRoleEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(IdentityRole.Id)]);
    }
}