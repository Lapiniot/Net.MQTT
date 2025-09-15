using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1822 // Mark members as static

namespace Mqtt.Server.Identity;

public static class DbContextOptionsBuilderExtensions
{

    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder WithConvention<T>() where T : IConvention
        {
            ArgumentNullException.ThrowIfNull(builder);

            ((IDbContextOptionsBuilderInfrastructure)builder)
                .AddOrUpdateExtension<AddConventionSetPluginExtension<T>>(extension: new());

            return builder;
        }

        public DbContextOptionsBuilder WithConvention<T>(Func<IServiceProvider, T> factory) where T : IConvention
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(factory);

            ((IDbContextOptionsBuilderInfrastructure)builder)
                .AddOrUpdateExtension<AddConventionSetPluginExtension<T>>(extension: new(factory));

            return builder;
        }
    }
}

internal sealed class AddConventionSetPluginExtension<T>(Func<IServiceProvider, T>? factory = null) :
    IDbContextOptionsExtension where T : IConvention
{
    private readonly Func<IServiceProvider, T>? factory = factory;

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        var builder = new EntityFrameworkServicesBuilder(services);
        if (factory is not null)
        {
            builder.TryAdd<IConventionSetPlugin, ConventionSetPlugin<T>>(
                factory: sp => new ConventionSetPlugin<T>(factory(sp)));
        }
        else
        {
            builder.TryAdd<IConventionSetPlugin, ConventionSetPlugin<T>>(
                factory: sp => new ConventionSetPlugin<T>(ActivatorUtilities.GetServiceOrCreateInstance<T>(sp)));
        }
    }

    public void Validate(IDbContextOptions options) { }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => $"Adds custom convention of type '{typeof(T)}'";

        public override int GetServiceProviderHashCode() => 0;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) { }
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
    }
}

internal sealed class ConventionSetPlugin<T>(T convention) : IConventionSetPlugin where T : IConvention
{
    private readonly T convention = convention;

    ConventionSet IConventionSetPlugin.ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.Add(convention);
        return conventionSet;
    }
}