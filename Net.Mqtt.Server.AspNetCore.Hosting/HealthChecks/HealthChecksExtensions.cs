using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

public static class HealthChecksExtensions
{
    private static readonly JsonContext JsonContext = new(new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new HealthReportJsonConverter() }
    });

    public static IHealthChecksBuilder AddMemoryCheck(this IHealthChecksBuilder builder, string tag = "memory") =>
        builder.AddCheck<MemoryHealthCheck>("MemoryCheck", HealthStatus.Unhealthy, [tag]);

    public static IEndpointConventionBuilder MapMemoryHealthCheck(this IEndpointRouteBuilder endpoints, string pattern, string tag = "memory") =>
        endpoints.MapHealthChecks(pattern, new()
        {
            Predicate = check => check.Tags.Contains(tag),
            ResponseWriter = WriteAsJsonAsync,
            AllowCachingResponses = false
        });

    private static Task WriteAsJsonAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(report, JsonContext.HealthReport, null, context.RequestAborted);
}

[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(HealthReport))]
internal sealed partial class JsonContext : JsonSerializerContext { }