using Microsoft.EntityFrameworkCore.Design;

namespace Mqtt.Server.Identity.CosmosDB;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureCosmos("AccountEndpoint=http://localhost:57220", databaseName: "app-db").Options);
}