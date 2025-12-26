using Microsoft.Extensions.DependencyInjection;
using Net.Mqtt.Server;

#pragma warning disable CA1812 // Internal class is never instantiated

namespace Mqtt.Server.Identity;

internal sealed class IdentityMqttAuthenticationHandler(IServiceProvider serviceProvider) : IMqttAuthenticationHandler
{
    async ValueTask<bool> IMqttAuthenticationHandler.AuthenticateAsync(string userName, string password)
    {
        using var serviceScope = serviceProvider.CreateScope();
        var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(userName).ConfigureAwait(false);
        return user is not null
            && await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false)
            && !await userManager.IsLockedOutAsync(user).ConfigureAwait(false);
    }
}