#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Mqtt.Server.Identity.CosmosDB;

internal sealed class IdentityModelConfigurationConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        modelBuilder.HasDefaultContainer("Identity");

        if (modelBuilder.Metadata.FindEntityType(typeof(IdentityRole)) is { } roleEntity)
        {
            var builder = roleEntity.Builder;
            builder.ToContainer("Identity_Roles");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(IdentityRole.Id)]);
            roleEntity.FindProperty(nameof(IdentityRole.ConcurrencyStamp))?.Builder
                .IsETagConcurrency();

            if (roleEntity.FindProperty(nameof(IdentityRole.NormalizedName)) is { } nameProperty)
            {
                builder.Metadata.RemoveIndex([nameProperty]);
            }
        }

        if (modelBuilder.Metadata.FindEntityType(typeof(IdentityRoleClaim<string>)) is { } roleClaimEntity)
        {
            var builder = roleClaimEntity.Builder;
            builder.ToContainer("Identity_RoleClaims");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(IdentityRoleClaim<>.RoleId)]);

            if (roleClaimEntity.FindProperty(nameof(IdentityRoleClaim<>.RoleId)) is { } roleIdProperty)
            {
                builder.Metadata.RemoveIndex([roleIdProperty]);
            }
        }

        if (modelBuilder.Metadata.FindEntityType(typeof(IdentityUserClaim<string>)) is { } userClaimEntity)
        {
            var builder = userClaimEntity.Builder;
            builder.ToContainer("Identity_UserClaims");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(IdentityUserClaim<>.UserId)]);

            if (userClaimEntity.FindProperty(nameof(IdentityUserClaim<>.UserId)) is { } userIdProperty)
            {
                builder.Metadata.RemoveIndex([userIdProperty]);
            }
        }

        if (modelBuilder.Metadata.FindEntityType(typeof(IdentityUserLogin<string>)) is { } userLoginEntity)
        {
            var builder = userLoginEntity.Builder;
            builder.ToContainer("Identity_UserLogins");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(IdentityUserLogin<>.UserId)]);

            if (userLoginEntity.FindProperty(nameof(IdentityUserLogin<>.UserId)) is { } userIdProperty)
            {
                builder.Metadata.RemoveIndex([userIdProperty]);
            }
        }

        if (modelBuilder.Metadata.FindEntityType(typeof(IdentityUserRole<string>)) is { } userRoleEntity)
        {
            var builder = userRoleEntity.Builder;
            builder.ToContainer("Identity_UserRoles");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(IdentityUserRole<>.UserId)]);

            if (userRoleEntity.FindProperty(nameof(IdentityUserRole<>.RoleId)) is { } roleIdProperty)
            {
                builder.Metadata.RemoveIndex([roleIdProperty]);
            }
        }

        if (modelBuilder.Metadata.FindEntityType(typeof(IdentityUserToken<string>)) is { } userTokenEntity)
        {
            var builder = userTokenEntity.Builder;
            builder.ToContainer("Identity_UserTokens");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(IdentityUserToken<>.UserId)]);
        }

        if (modelBuilder.Metadata.FindEntityType(typeof(ApplicationUser)) is { } userEntity)
        {
            var builder = userEntity.Builder;
            builder.ToContainer("Identity_Users");
            builder.HasNoDiscriminator(fromDataAnnotation: true);
            builder.HasPartitionKey([nameof(ApplicationUser.Id)]);

            userEntity.FindProperty(nameof(ApplicationUser.ConcurrencyStamp))?.Builder
                .IsETagConcurrency();

            if (userEntity.FindProperty(nameof(ApplicationUser.NormalizedUserName)) is { } nameProperty)
            {
                builder.Metadata.RemoveIndex([nameProperty]);
            }

            if (userEntity.FindProperty(nameof(ApplicationUser.NormalizedEmail)) is { } emailProperty)
            {
                builder.Metadata.RemoveIndex([emailProperty]);
            }
        }
    }
}