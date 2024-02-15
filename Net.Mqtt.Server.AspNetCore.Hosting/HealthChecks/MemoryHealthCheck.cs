using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

#pragma warning disable CA1812

internal sealed class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var info = GC.GetGCMemoryInfo();
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, "Memory usage status", null,
            new Dictionary<string, object>
            {
                { "Total", GC.GetTotalMemory(false) },
                { "TotalAllocated", GC.GetTotalAllocatedBytes() },
                { "TotalCommittedBytes", info.TotalCommittedBytes },
                { "Gen0Collections", GC.CollectionCount(0) },
                { "Gen1Collections", GC.CollectionCount(1) },
                { "Gen2Collections", GC.CollectionCount(2) }
            }));
    }
}