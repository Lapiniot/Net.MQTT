using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1708 // Identifiers should differ by more than case

public static class HealthChecksExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        public IHealthChecksBuilder AddMemoryCheck(string tag = "memory") =>
        builder.AddCheck<MemoryHealthCheck>("MemoryCheck", HealthStatus.Unhealthy, [tag]);
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointConventionBuilder MapMemoryHealthCheck(string pattern, string tag = "memory") =>
        endpoints.MapHealthChecks(pattern, new()
        {
            Predicate = check => check.Tags.Contains(tag),
            ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(report,
                HealthChecksJsonContext.Default.HealthReport, null, context.RequestAborted),
            AllowCachingResponses = false
        });
    }
}