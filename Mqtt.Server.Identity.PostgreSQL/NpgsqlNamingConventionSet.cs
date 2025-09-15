using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using static Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator;

#pragma warning disable CA1812

namespace Mqtt.Server.Identity.PostgreSQL;

internal sealed class NpgsqlNamingConventionSet : IEntityTypeAddedConvention, IPropertyAddedConvention,
    IIndexAddedConvention, IKeyAddedConvention, IForeignKeyAddedConvention, ITriggerAddedConvention
{
    void IEntityTypeAddedConvention.ProcessEntityTypeAdded(IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        if (entityTypeBuilder.Metadata is var metadata && metadata.GetDefaultTableName() is { } name)
        {
            entityTypeBuilder.ToTable(ConvertToSnakeCase(name), metadata.GetSchema());
        }
    }

    void IPropertyAddedConvention.ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
    {
        if (propertyBuilder.Metadata is var metadata && metadata.GetDefaultColumnName() is { } name)
        {
            propertyBuilder.HasColumnName(ConvertToSnakeCase(name));
        }
    }

    void IIndexAddedConvention.ProcessIndexAdded(IConventionIndexBuilder indexBuilder,
        IConventionContext<IConventionIndexBuilder> context)
    {
        if (indexBuilder.Metadata is var metadata && metadata.GetDefaultDatabaseName() is { } name)
        {
            indexBuilder.HasDatabaseName(ConvertToSnakeCase(name));
        }
    }

    void IKeyAddedConvention.ProcessKeyAdded(IConventionKeyBuilder keyBuilder,
        IConventionContext<IConventionKeyBuilder> context)
    {
        if (keyBuilder.Metadata is var metadata && metadata.GetDefaultName() is { } name)
        {
            keyBuilder.HasName(ConvertToSnakeCase(name));
        }
    }

    void IForeignKeyAddedConvention.ProcessForeignKeyAdded(IConventionForeignKeyBuilder foreignKeyBuilder,
        IConventionContext<IConventionForeignKeyBuilder> context)
    {
        if (foreignKeyBuilder.Metadata is var metadata && metadata.GetDefaultName() is { } name)
        {
            foreignKeyBuilder.HasConstraintName(ConvertToSnakeCase(name));
        }
    }

    void ITriggerAddedConvention.ProcessTriggerAdded(IConventionTriggerBuilder triggerBuilder,
        IConventionContext<IConventionTriggerBuilder> context)
    {
        if (triggerBuilder.Metadata is var metadata && metadata.GetDefaultDatabaseName() is { } name)
        {
            triggerBuilder.HasDatabaseName(ConvertToSnakeCase(name));
        }
    }
}