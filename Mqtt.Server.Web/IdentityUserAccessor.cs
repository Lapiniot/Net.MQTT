using Microsoft.AspNetCore.Identity;
using Mqtt.Server.Identity.Data;
using Microsoft.AspNetCore.Http;

namespace Mqtt.Server.Web;

#pragma warning disable CA1812
internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);

        if (user is null)
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);

        return user;
    }
}