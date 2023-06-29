namespace System.Net.Mqtt;

public sealed class ByteSequenceComparer : IEqualityComparer<ReadOnlyMemory<byte>>, IEqualityComparer<byte[]>
{
    public static ByteSequenceComparer Instance { get; } = new();

    #region Implementation of IEqualityComparer<in ReadOnlyMemory<byte>>

    /// <inheritdoc />
    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) => x.Span.SequenceEqual(y.Span);

    /// <inheritdoc />
    public int GetHashCode(ReadOnlyMemory<byte> obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj.Span);
        return hash.ToHashCode();
    }

    #endregion

    #region Implementation of IEqualityComparer<in byte[]>

    /// <inheritdoc />
    public bool Equals(byte[] x, byte[] y) => x.AsSpan().SequenceEqual(y);

    /// <inheritdoc />
    public int GetHashCode(byte[] obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj);
        return hash.ToHashCode();
    }

    #endregion
}