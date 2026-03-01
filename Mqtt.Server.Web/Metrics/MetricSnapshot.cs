namespace Mqtt.Server.Web.Metrics;

public sealed class MetricSnapshot<T>(string name, string? description, bool enabled = true) :
    Metric<T>(name, description, enabled) where T : struct
{
    public T Current { get; private set; }

    protected internal override void Update(T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        Current = measurement;
    }
}