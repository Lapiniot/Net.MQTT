#pragma warning disable CA1822, CA1819

namespace System.Net.Mqtt.Benchmarks;

public class SampleSet<T>
{
    public SampleSet(string name, T[] samples)
    {
        DisplayName = name;
        Samples = samples;
    }

    public string DisplayName { get; }
    public T[] Samples { get; }

    public override string ToString() => DisplayName;
}