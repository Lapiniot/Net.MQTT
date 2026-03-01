namespace Mqtt.Server.Web.Metrics;

public abstract class Metric(string name, string? description, bool enabled = true)
{
    public string? Description { get; } = description;
    public bool Enabled { get; protected internal set; } = enabled;
    public string Name { get; } = name;
}

public abstract class Metric<T>(string name, string? description, bool enabled = true) :
    Metric(name, description, enabled) where T : struct
{
    protected internal abstract void Update(T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags);
}