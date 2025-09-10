using Microsoft.EntityFrameworkCore.Design;

namespace Mqtt.Server.Identity.Sqlite;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=app.db",
                b => b.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly)).Options);
}