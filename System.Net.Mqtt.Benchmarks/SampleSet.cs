#pragma warning disable CA1822, CA1819, CA1019

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

namespace System.Net.Mqtt.Benchmarks;

public abstract class SampleSetBase
{
    protected SampleSetBase(string name) => Name = name;

    public string Name { get; }

    public override string ToString() => Name;
}

public class SampleSet<T> : SampleSetBase
{
    public SampleSet(string name, T[] samples) : base(name) => Samples = samples;

    public T[] Samples { get; }
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class SampleSetsFilterAttribute : FilterConfigBaseAttribute
{
    public SampleSetsFilterAttribute(params string[] sampleSets) :
        base(new SampleSetsFilter(sampleSets))
    { }
}

internal sealed class SampleSetsFilter : IFilter
{
    private readonly string[] sampleSets;

    public SampleSetsFilter(params string[] sampleSets) => this.sampleSets = sampleSets;

    public bool Predicate(BenchmarkCase benchmarkCase) =>
        benchmarkCase is not { HasArguments: true, Parameters.Items: [{ Value: SampleSetBase { Name: var displayName } }, ..] }
        || sampleSets.Contains(displayName, StringComparer.OrdinalIgnoreCase);
}