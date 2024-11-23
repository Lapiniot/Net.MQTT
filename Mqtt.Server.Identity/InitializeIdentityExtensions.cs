using Microsoft.Extensions.DependencyInjection;

namespace Mqtt.Server.Identity;

public static class InitializeIdentityExtensions
{
    public static async Task InitializeIdentityStoreAsync(IServiceProvider serviceProvider,
        Func<ApplicationDbContext, IServiceProvider, Task>? customSeedAction = null)
    {
        var scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using (ctx.ConfigureAwait(false))
            {
                await ctx.Database.MigrateAsync().ConfigureAwait(false);
                if (customSeedAction is not null)
                {
                    await customSeedAction(ctx, scope.ServiceProvider).ConfigureAwait(false);
                }
            }
        }
    }
}