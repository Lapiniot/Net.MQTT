using System.Diagnostics.CodeAnalysis;

namespace System.Net.Mqtt;

public class Utf8StringComparer : IEqualityComparer<Utf8String>
{
    public static Utf8StringComparer Instance { get; } = new Utf8StringComparer();

    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) => x.Span.SequenceEqual(y.Span);

    public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj.Span);
        return hash.ToHashCode();
    }
}