namespace Mqtt.Server.Identity.Sqlite;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

public static class DbContextOptionsBuilderExtensions
{
    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder ConfigureSqlite(string connectionString)
        {
            return builder
#if !NET10_0_OR_GREATER
                .UseModel(Compiled.ApplicationDbContextModel.Instance)
#endif
                .UseSqlite(connectionString, options => options
                    .MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly));
        }
    }
}