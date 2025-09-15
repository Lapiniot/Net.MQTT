namespace Mqtt.Server.Identity.PostgreSQL;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

public static class DbContextOptionsBuilderExtensions
{
    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder ConfigureNpgsql(string connectionString)
        {
            return builder
                .UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly));
        }
    }
}