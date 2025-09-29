using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Mqtt.Server.Identity.CosmosDB;

internal sealed class CosmosUserStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null) :
    UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>(context, describer)
{
    public override async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        var roleIds = await Context.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == user.Id)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Context.Roles
                .AsNoTracking()
                .Where(role => roleIds.Contains(role.Id))
                .Select(role => role.Name!)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.InternalServerError)
        {
            // Special client-side evaluation fallback for semi-functional Azure Cosmos DB Linux emulator, 
            // which doesn't have some critical features implemented at the momment.
            // We specifically need either ARRAY_CONTAINS function or nested queries support to get all roles 
            // in one server-side evaluated query.
            return [.. (await Context.Roles
                .AsNoTracking()
                .Select(role => new { role.Id, role.Name })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
                    .Where(role => roleIds.Contains(role.Id))
                    .Select(role => role.Name!)];
        }
    }

    public override async Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(claim);

        var userIds = await Context.UserClaims
            .AsNoTracking()
            .Where(userClaim => userClaim.ClaimValue == claim.Value && userClaim.ClaimType == claim.Type)
            .Select(userClaim => userClaim.UserId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Context.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.InternalServerError)
        {
            // Special client-side evaluation fallback for semi-functional Azure Cosmos DB Linux emulator
            return [.. (await Context.Users
                .AsNoTracking()
                .ToListAsync(cancellationToken).ConfigureAwait(false))
                .Where(user => userIds.Contains(user.Id))];
        }
    }

    public override async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(normalizedRoleName);

        var roleId = await Context.Roles
            .AsNoTracking()
            .Where(role => role.NormalizedName == normalizedRoleName)
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (roleId is not null)
        {
            var userIds = await Context.UserRoles
                .AsNoTracking()
                .Where(userRole => userRole.RoleId == roleId)
                .Select(userRole => userRole.UserId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await Context.Users
                    .AsNoTracking()
                    .Where(user => userIds.Contains(user.Id))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.InternalServerError)
            {
                // Special client-side evaluation fallback for semi-functional Azure Cosmos DB Linux emulator
                return [.. (await Context.Users
                    .AsNoTracking()
                    .ToListAsync(cancellationToken).ConfigureAwait(false))
                    .Where(user => userIds.Contains(user.Id))];
            }
        }

        return [];
    }
}