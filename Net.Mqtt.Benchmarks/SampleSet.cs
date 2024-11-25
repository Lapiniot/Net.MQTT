using System.Collections.Immutable;

namespace Net.Mqtt.Benchmarks;

public record SampleSet<T>(string Name, params ImmutableArray<T> Samples)
{
    public override string ToString() => Name;
}