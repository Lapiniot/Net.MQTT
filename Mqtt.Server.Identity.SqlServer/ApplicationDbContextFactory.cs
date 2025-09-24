using Microsoft.EntityFrameworkCore.Design;

namespace Mqtt.Server.Identity.SqlServer;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureSqlServer("Server=localhost;Database=mqtt-server-db;User Id=sa;Password=pwd;").Options);
}