namespace Mqtt.Server.Web;

public sealed class MetricRecord<T>(string name, string? description, bool enabled = true) :
    MetricRecord(name, description, enabled) where T : struct
{
    public T Value { get; internal set; }
}

public abstract class MetricRecord(string name, string? description, bool enabled = true)
{
    public string? Description { get; } = description;
    public bool Enabled { get; protected internal set; } = enabled;
    public string Name { get; } = name;
}