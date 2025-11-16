using Microsoft.EntityFrameworkCore.Metadata;
using static Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator;

#pragma warning disable CA1812

namespace Mqtt.Server.Identity.PostgreSQL;

internal sealed class NpgsqlNamingConventionSet :
    IEntityTypeAddedConvention,
    IEntityTypeAnnotationChangedConvention,
    IPropertyAddedConvention,
    IIndexAddedConvention,
    IKeyAddedConvention,
    IForeignKeyAddedConvention,
    ITriggerAddedConvention,
    IForeignKeyOwnershipChangedConvention
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

    void IForeignKeyOwnershipChangedConvention.ProcessForeignKeyOwnershipChanged(
        IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<bool?> context)
    {
        if (relationshipBuilder.Metadata is { IsOwnership: true, DeclaringEntityType: var owned }
            && owned.IsMappedToJson())
        {
            ResetExplicitelyAssignedNames(owned);
        }
    }

    void IEntityTypeAnnotationChangedConvention.ProcessEntityTypeAnnotationChanged(
        IConventionEntityTypeBuilder entityTypeBuilder, string name, IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation, IConventionContext<IConventionAnnotation> context)
    {
        if (name == RelationalAnnotationNames.ContainerColumnName
            && entityTypeBuilder.Metadata.FindOwnership() is { DeclaringEntityType: var owned }
            && owned.IsMappedToJson())
        {
            ResetExplicitelyAssignedNames(owned);
        }
    }

    private static void ResetExplicitelyAssignedNames(IConventionEntityType entity)
    {
        var builder = entity.Builder;
        builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
        builder.HasNoAnnotation(RelationalAnnotationNames.Schema);

        var containerColumnName = entity.GetContainerColumnName();
        entity.SetContainerColumnName(ConvertToSnakeCase(containerColumnName!));

        foreach (var property in entity.GetProperties())
        {
            property.Builder.HasNoAnnotation(RelationalAnnotationNames.ColumnName);
        }
    }
}