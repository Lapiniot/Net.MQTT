using System.Diagnostics;

namespace Mqtt.Server.Web.Metrics;

public sealed class CpuTimeMetricSnapshot(string name, string? description, bool enabled = true) :
    Metric<double>(name, description, enabled)
{
    private static readonly double multiplier = (double)Stopwatch.Frequency / Environment.ProcessorCount;
    private long userWallTime;
    private long systemWallTime;
    private double userTime;
    private double systemTime;
    private double userUsage;
    private double systemUsage;

    public double UserUsage => userUsage;
    public double UserTime => userTime;
    public double SystemUsage => systemUsage;
    public double SystemTime => systemTime;
    public double TotalUsage => userUsage + systemUsage;

    protected internal override void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags is [("cpu.mode", string mode), ..])
        {
            switch (mode)
            {
                case "user":
                    Update(measurement, ref userTime, ref userWallTime, ref userUsage);
                    break;
                case "system":
                    Update(measurement, ref systemTime, ref systemWallTime, ref systemUsage);
                    break;
            }
        }
    }

    private static void Update(double measurement, ref double currentTime, ref long currentWallTime, ref double usage)
    {
        var lastTime = currentTime;
        var lastWallTime = currentWallTime;
        currentTime = measurement;
        currentWallTime = Stopwatch.GetTimestamp();
        usage = (currentTime - lastTime) * multiplier / (currentWallTime - lastWallTime);
    }
}