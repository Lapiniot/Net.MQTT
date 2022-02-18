using System.Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting.Configuration;

public static class HealthChecksExtensions
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new HealthReportJsonConverter() }
    };

    public static IHealthChecksBuilder AddMemoryCheck(this IHealthChecksBuilder builder, string tag = "memory") => builder.AddCheck<MemoryHealthCheck>("MemoryCheck", HealthStatus.Unhealthy, new[] { tag });

    public static IEndpointConventionBuilder MapMemoryHealthCheck(this IEndpointRouteBuilder endpoints, string pattern, string tag = "memory")
    {
        return endpoints.MapHealthChecks(pattern, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(tag),
            ResponseWriter = WriteAsJsonAsync,
            AllowCachingResponses = false
        });
    }

    private static Task WriteAsJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(report, jsonOptions, context.RequestAborted);
    }
}