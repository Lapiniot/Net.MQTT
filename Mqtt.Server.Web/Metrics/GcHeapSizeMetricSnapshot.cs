namespace Mqtt.Server.Web.Metrics;

public sealed class GcHeapSizeMetricSnapshot(string name, string? description, bool enabled = true) :
    Metric<long>(name, description, enabled)
{
    public long Gen0Size { get; private set; }
    public long Gen1Size { get; private set; }
    public long Gen2Size { get; private set; }
    public long LohSize { get; private set; }
    public long PohSize { get; private set; }

    protected internal override void Update(long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags is [("gc.heap.generation", string generation), ..])
        {
            switch (generation)
            {
                case "gen0": Gen0Size = measurement; break;
                case "gen1": Gen1Size = measurement; break;
                case "gen2": Gen2Size = measurement; break;
                case "loh": LohSize = measurement; break;
                case "poh": PohSize = measurement; break;
            }
        }
    }
}