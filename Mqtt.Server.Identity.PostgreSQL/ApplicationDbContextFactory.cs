using Microsoft.EntityFrameworkCore.Design;

namespace Mqtt.Server.Identity.PostgreSQL;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureNpgsql("Host=localhost;Port=5432;Database=mqtt-server")
            .Options);
}