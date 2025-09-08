using Microsoft.AspNetCore.OutputCaching;

namespace Mqtt.Server;

/// <summary>
/// A policy which caches GET and HEAD, 200 responses.
/// This is essentially the same as <see cref="DefaultPolicy"/>, 
/// but allows caching even for authenticated requests with Set-Cookie headers.
/// </summary>
internal sealed class AllowCachingAuthenticatedPolicy : IOutputCachePolicy
{
    public static AllowCachingAuthenticatedPolicy Instance { get; } = new();

    public ValueTask CacheRequestAsync([NotNull] OutputCacheContext context, CancellationToken cancellation)
    {
        var attemptOutputCaching = context.HttpContext.Request is { Method: "GET" or "HEAD" };
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Vary by any query by default
        context.CacheVaryByRules.QueryKeys = "*";

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync([NotNull] OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        // Check response code
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }
}