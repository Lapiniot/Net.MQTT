using static Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator;

namespace Mqtt.Server.Identity.PostgreSQL;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

public static class DbContextOptionsBuilderExtensions
{
    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder ConfigureNpgsql(string connectionString)
        {
            return builder
#if !NET10_0_OR_GREATER
                .UseModel(Compiled.ApplicationDbContextModel.Instance)
#endif
                .UseNpgsql(connectionString, options => options
                    .MigrationsHistoryTable(ConvertToSnakeCase("__EFMigrationsHistory"))
                    .MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly))
                .WithConvention<NpgsqlNamingConventionSet>()
                .WithConvention<CustomizeIdentityModelConvention>();
        }
    }
}

internal sealed class CustomizeIdentityModelConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        // Deliberately convert all entity names to snake-case to match PostgreSQL naming conventions.
        // ASP.NET Identity libarary explicitely assigns hard-coded names for all model entities and some indexes via 
        // fluent API ToTable("table-name") calls in the OnModelCreating():
        // https://github.com/dotnet/aspnetcore/blob/7dbebe9df712157b5bce268c4244cfcbaa2a26d4/src/Identity/EntityFrameworkCore/src/IdentityDbContext.cs#L222C13-L222C38
        // Thus, the only place where we can forcibly update entity names is here, 
        // in the custom IModelFinalizingConvention, as it is our last chance to 
        // apply model modifications before it is finalized. 
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            if (entity.GetTableName() is { } name)
            {
                entity.Builder.Metadata.SetTableName(ConvertToSnakeCase(name));

                foreach (var idx in entity.Builder.Metadata.GetIndexes())
                {
                    if (idx.GetDatabaseName() is { } idxName)
                    {
                        idx.SetDatabaseName(ConvertToSnakeCase(idxName));
                    }
                }
            }
        }
    }
}