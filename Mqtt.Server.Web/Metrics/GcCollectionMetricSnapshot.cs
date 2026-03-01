namespace Mqtt.Server.Web.Metrics;

public sealed class GcCollectionMetricSnapshot(string name, string? description, bool enabled = true) :
    Metric<long>(name, description, enabled)
{
    public long Gen0GcCollections { get; private set; }
    public long Gen1GcCollections { get; private set; }
    public long Gen2GcCollections { get; private set; }

    protected internal override void Update(long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags is [("gc.heap.generation", string generation), ..])
        {
            switch (generation)
            {
                case "gen0": Gen0GcCollections = measurement; break;
                case "gen1": Gen1GcCollections = measurement; break;
                case "gen2": Gen2GcCollections = measurement; break;
            }
        }
    }
}