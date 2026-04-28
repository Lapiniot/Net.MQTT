using System.Collections.Immutable;

#nullable enable

namespace Net.Mqtt.Benchmarks;

public record SampleSet<T>(string Name, params ImmutableArray<T> Samples)
{
    public override string ToString() => Name;
}

public record Sample<T>(string Name, T Value)
{
    public override string ToString() => Name;

#pragma warning disable CA2225 // Operator overloads have named alternates
    public static implicit operator T([NotNull] Sample<T> sample) => sample.Value;
#pragma warning restore CA2225 // Operator overloads have named alternates
}