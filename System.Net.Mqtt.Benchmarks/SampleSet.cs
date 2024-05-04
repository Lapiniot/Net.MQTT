#pragma warning disable CA1822, CA1819, CA1019

namespace System.Net.Mqtt.Benchmarks;

public abstract class SampleSetBase
{
    protected SampleSetBase(string name) => Name = name;

    public string Name { get; }

    public override string ToString() => Name;
}

public class SampleSet<T>(string name, T[] samples) : SampleSetBase(name)
{
    public T[] Samples { get; } = samples;
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class SampleSetsFilterAttribute(params string[] sampleSets) : FilterConfigBaseAttribute(new SampleSetsFilter(sampleSets))
{ }

internal sealed class SampleSetsFilter(params string[] sampleSets) : IFilter
{
    public bool Predicate(BenchmarkCase benchmarkCase) =>
        benchmarkCase is not { HasArguments: true, Parameters.Items: [{ Value: SampleSetBase { Name: var displayName } }, ..] }
        || sampleSets.Contains(displayName, StringComparer.OrdinalIgnoreCase);
}