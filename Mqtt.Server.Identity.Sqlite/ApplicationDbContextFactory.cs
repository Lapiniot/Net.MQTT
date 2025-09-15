using Microsoft.EntityFrameworkCore.Design;

namespace Mqtt.Server.Identity.Sqlite;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureSqlite("Data Source=app.db").Options);
}