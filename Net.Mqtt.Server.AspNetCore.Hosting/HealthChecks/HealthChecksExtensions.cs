using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

public static class HealthChecksExtensions
{
    public static IHealthChecksBuilder AddMemoryCheck(this IHealthChecksBuilder builder, string tag = "memory") =>
        builder.AddCheck<MemoryHealthCheck>("MemoryCheck", HealthStatus.Unhealthy, [tag]);

    public static IEndpointConventionBuilder MapMemoryHealthCheck(this IEndpointRouteBuilder endpoints, string pattern, string tag = "memory") =>
        endpoints.MapHealthChecks(pattern, new()
        {
            Predicate = check => check.Tags.Contains(tag),
            ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(report,
                HealthChecksJsonContext.Default.HealthReport, null, context.RequestAborted),
            AllowCachingResponses = false
        });
}