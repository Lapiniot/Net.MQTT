using Microsoft.EntityFrameworkCore;

namespace Mqtt.Server;

internal static class ApplicationInitializer
{
    public static async Task InitializeDbAsync<TContext>(this WebApplication app, Func<TContext, IServiceProvider, Task>? customSeedAction = null)
        where TContext : DbContext
    {
        await using var scope = app.Services.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        if (customSeedAction is not null)
        {
            await customSeedAction(dbContext, scope.ServiceProvider).ConfigureAwait(false);
        }
    }
}