namespace Mqtt.Server.Identity.SqlServer;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

public static class DbContextOptionsBuilderExtensions
{
    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder ConfigureSqlServer(string connectionString)
        {
            return builder
                .UseModel(Compiled.ApplicationDbContextModel.Instance)
                .UseSqlServer(connectionString, options => options
                    .MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly));
        }
    }
}