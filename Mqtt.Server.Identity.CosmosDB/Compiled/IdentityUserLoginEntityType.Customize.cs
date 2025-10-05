namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class IdentityUserLoginEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(IdentityUserLogin<>.UserId)]);
    }
}