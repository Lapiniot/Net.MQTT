namespace Mqtt.Server.Identity.CosmosDB.Compiled;

public partial class ApplicationUserEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Cosmos:PartitionKeyNames", (IReadOnlyList<string>)[nameof(ApplicationUser.Id)]);
    }
}