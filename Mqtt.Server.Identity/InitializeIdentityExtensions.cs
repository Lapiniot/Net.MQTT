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
                var database = ctx.Database;

                if (database.IsRelational())
                {
                    // Classical EFCore migrations work only with relational databases.
                    await database.MigrateAsync().ConfigureAwait(false);
                }
                else
                {
                    // The best what we can do for non-relational databases is to ensure db or container 
                    // (whatever it is called in the target nosql database terminology) 
                    // is created upon the first access attempt.
                    await database.EnsureCreatedAsync().ConfigureAwait(false);
                }

                if (customSeedAction is not null)
                {
                    await customSeedAction(ctx, scope.ServiceProvider).ConfigureAwait(false);
                }
            }
        }
    }
}