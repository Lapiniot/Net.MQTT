﻿// <auto-generated />
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Mqtt.Server.Identity.Data.Compiled
{
    [EntityFrameworkInternal]
    public partial class IdentityUserLoginEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "Microsoft.AspNetCore.Identity.IdentityUserLogin<string>",
                typeof(IdentityUserLogin<string>),
                baseEntityType,
                propertyCount: 4,
                foreignKeyCount: 1,
                unnamedIndexCount: 1,
                keyCount: 1);

            var loginProvider = runtimeEntityType.AddProperty(
                "LoginProvider",
                typeof(string),
                propertyInfo: typeof(IdentityUserLogin<string>).GetProperty("LoginProvider", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(IdentityUserLogin<string>).GetField("<LoginProvider>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                afterSaveBehavior: PropertySaveBehavior.Throw);
            loginProvider.SetGetter(
                string (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(entity),
                bool (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(entity) == null,
                string (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(instance),
                bool (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(instance) == null);
            loginProvider.SetSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(entity) = value);
            loginProvider.SetMaterializationSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(entity) = value);
            loginProvider.SetAccessors(
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.LoginProvider(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => entry.ReadOriginalValue<string>(loginProvider, 0),
                string (InternalEntityEntry entry) => entry.ReadRelationshipSnapshotValue<string>(loginProvider, 0),
                object (ValueBuffer valueBuffer) => valueBuffer[0]);
            loginProvider.SetPropertyIndexes(
                index: 0,
                originalValueIndex: 0,
                shadowIndex: -1,
                relationshipIndex: 0,
                storeGenerationIndex: -1);
            loginProvider.TypeMapping = SqliteStringTypeMapping.Default;
            loginProvider.SetCurrentValueComparer(new EntryCurrentValueComparer<string>(loginProvider));

            var providerKey = runtimeEntityType.AddProperty(
                "ProviderKey",
                typeof(string),
                propertyInfo: typeof(IdentityUserLogin<string>).GetProperty("ProviderKey", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(IdentityUserLogin<string>).GetField("<ProviderKey>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                afterSaveBehavior: PropertySaveBehavior.Throw);
            providerKey.SetGetter(
                string (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(entity),
                bool (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(entity) == null,
                string (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(instance),
                bool (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(instance) == null);
            providerKey.SetSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(entity) = value);
            providerKey.SetMaterializationSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(entity) = value);
            providerKey.SetAccessors(
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.ProviderKey(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => entry.ReadOriginalValue<string>(providerKey, 1),
                string (InternalEntityEntry entry) => entry.ReadRelationshipSnapshotValue<string>(providerKey, 1),
                object (ValueBuffer valueBuffer) => valueBuffer[1]);
            providerKey.SetPropertyIndexes(
                index: 1,
                originalValueIndex: 1,
                shadowIndex: -1,
                relationshipIndex: 1,
                storeGenerationIndex: -1);
            providerKey.TypeMapping = SqliteStringTypeMapping.Default;
            providerKey.SetCurrentValueComparer(new EntryCurrentValueComparer<string>(providerKey));

            var providerDisplayName = runtimeEntityType.AddProperty(
                "ProviderDisplayName",
                typeof(string),
                propertyInfo: typeof(IdentityUserLogin<string>).GetProperty("ProviderDisplayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(IdentityUserLogin<string>).GetField("<ProviderDisplayName>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);
            providerDisplayName.SetGetter(
                string (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(entity),
                bool (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(entity) == null,
                string (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(instance),
                bool (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(instance) == null);
            providerDisplayName.SetSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(entity) = value);
            providerDisplayName.SetMaterializationSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(entity) = value);
            providerDisplayName.SetAccessors(
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.ProviderDisplayName(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => entry.ReadOriginalValue<string>(providerDisplayName, 2),
                string (InternalEntityEntry entry) => entry.GetCurrentValue<string>(providerDisplayName),
                object (ValueBuffer valueBuffer) => valueBuffer[2]);
            providerDisplayName.SetPropertyIndexes(
                index: 2,
                originalValueIndex: 2,
                shadowIndex: -1,
                relationshipIndex: -1,
                storeGenerationIndex: -1);
            providerDisplayName.TypeMapping = SqliteStringTypeMapping.Default;

            var userId = runtimeEntityType.AddProperty(
                "UserId",
                typeof(string),
                propertyInfo: typeof(IdentityUserLogin<string>).GetProperty("UserId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(IdentityUserLogin<string>).GetField("<UserId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            userId.SetGetter(
                string (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.UserId(entity),
                bool (IdentityUserLogin<string> entity) => IdentityUserLoginUnsafeAccessors<string>.UserId(entity) == null,
                string (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.UserId(instance),
                bool (IdentityUserLogin<string> instance) => IdentityUserLoginUnsafeAccessors<string>.UserId(instance) == null);
            userId.SetSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.UserId(entity) = value);
            userId.SetMaterializationSetter(
                (IdentityUserLogin<string> entity, string value) => IdentityUserLoginUnsafeAccessors<string>.UserId(entity) = value);
            userId.SetAccessors(
                string (InternalEntityEntry entry) => (entry.FlaggedAsStoreGenerated(3) ? entry.ReadStoreGeneratedValue<string>(0) : (entry.FlaggedAsTemporary(3) && IdentityUserLoginUnsafeAccessors<string>.UserId(((IdentityUserLogin<string>)(entry.Entity))) == null ? entry.ReadTemporaryValue<string>(0) : IdentityUserLoginUnsafeAccessors<string>.UserId(((IdentityUserLogin<string>)(entry.Entity))))),
                string (InternalEntityEntry entry) => IdentityUserLoginUnsafeAccessors<string>.UserId(((IdentityUserLogin<string>)(entry.Entity))),
                string (InternalEntityEntry entry) => entry.ReadOriginalValue<string>(userId, 3),
                string (InternalEntityEntry entry) => entry.ReadRelationshipSnapshotValue<string>(userId, 2),
                object (ValueBuffer valueBuffer) => valueBuffer[3]);
            userId.SetPropertyIndexes(
                index: 3,
                originalValueIndex: 3,
                shadowIndex: -1,
                relationshipIndex: 2,
                storeGenerationIndex: 0);
            userId.TypeMapping = SqliteStringTypeMapping.Default;
            userId.SetCurrentValueComparer(new EntryCurrentValueComparer<string>(userId));

            var key = runtimeEntityType.AddKey(
                new[] { loginProvider, providerKey });
            runtimeEntityType.SetPrimaryKey(key);

            var index = runtimeEntityType.AddIndex(
                new[] { userId });

            return runtimeEntityType;
        }

        public static RuntimeForeignKey CreateForeignKey1(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
        {
            var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("UserId") },
                principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Id") }),
                principalEntityType,
                deleteBehavior: DeleteBehavior.Cascade,
                required: true);

            return runtimeForeignKey;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            var loginProvider = runtimeEntityType.FindProperty("LoginProvider");
            var providerKey = runtimeEntityType.FindProperty("ProviderKey");
            var providerDisplayName = runtimeEntityType.FindProperty("ProviderDisplayName");
            var userId = runtimeEntityType.FindProperty("UserId");
            var key = runtimeEntityType.FindKey(new[] { loginProvider, providerKey });
            key.SetPrincipalKeyValueFactory(KeyValueFactoryFactory.CreateCompositeFactory(key));
            key.SetIdentityMapFactory(IdentityMapFactoryFactory.CreateFactory<IReadOnlyList<object>>(key));
            runtimeEntityType.SetOriginalValuesFactory(
                ISnapshot (InternalEntityEntry source) =>
                {
                    var entity = ((IdentityUserLogin<string>)(source.Entity));
                    return ((ISnapshot)(new Snapshot<string, string, string, string>((source.GetCurrentValue<string>(loginProvider) == null ? null : ((ValueComparer<string>)(((IProperty)loginProvider).GetValueComparer())).Snapshot(source.GetCurrentValue<string>(loginProvider))), (source.GetCurrentValue<string>(providerKey) == null ? null : ((ValueComparer<string>)(((IProperty)providerKey).GetValueComparer())).Snapshot(source.GetCurrentValue<string>(providerKey))), (source.GetCurrentValue<string>(providerDisplayName) == null ? null : ((ValueComparer<string>)(((IProperty)providerDisplayName).GetValueComparer())).Snapshot(source.GetCurrentValue<string>(providerDisplayName))), (source.GetCurrentValue<string>(userId) == null ? null : ((ValueComparer<string>)(((IProperty)userId).GetValueComparer())).Snapshot(source.GetCurrentValue<string>(userId))))));
                });
            runtimeEntityType.SetStoreGeneratedValuesFactory(
                ISnapshot () => ((ISnapshot)(new Snapshot<string>((default(string) == null ? null : ((ValueComparer<string>)(((IProperty)userId).GetValueComparer())).Snapshot(default(string)))))));
            runtimeEntityType.SetTemporaryValuesFactory(
                ISnapshot (InternalEntityEntry source) => ((ISnapshot)(new Snapshot<string>(default(string)))));
            runtimeEntityType.SetShadowValuesFactory(
                ISnapshot (IDictionary<string, object> source) => Snapshot.Empty);
            runtimeEntityType.SetEmptyShadowValuesFactory(
                ISnapshot () => Snapshot.Empty);
            runtimeEntityType.SetRelationshipSnapshotFactory(
                ISnapshot (InternalEntityEntry source) =>
                {
                    var entity = ((IdentityUserLogin<string>)(source.Entity));
                    return ((ISnapshot)(new Snapshot<string, string, string>((source.GetCurrentValue<string>(loginProvider) == null ? null : ((ValueComparer<string>)(((IProperty)loginProvider).GetKeyValueComparer())).Snapshot(source.GetCurrentValue<string>(loginProvider))), (source.GetCurrentValue<string>(providerKey) == null ? null : ((ValueComparer<string>)(((IProperty)providerKey).GetKeyValueComparer())).Snapshot(source.GetCurrentValue<string>(providerKey))), (source.GetCurrentValue<string>(userId) == null ? null : ((ValueComparer<string>)(((IProperty)userId).GetKeyValueComparer())).Snapshot(source.GetCurrentValue<string>(userId))))));
                });
            runtimeEntityType.Counts = new PropertyCounts(
                propertyCount: 4,
                navigationCount: 0,
                complexPropertyCount: 0,
                originalValueCount: 4,
                shadowCount: 0,
                relationshipCount: 3,
                storeGeneratedCount: 1);
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "AspNetUserLogins");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}