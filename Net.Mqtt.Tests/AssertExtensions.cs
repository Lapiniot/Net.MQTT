using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Net.Mqtt.Tests;

public static class AssertExtensions
{
    extension(Assert)
    {
        public static void AreSameRef<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual) =>
            Assert.IsTrue(Unsafe.AreSame(
                ref MemoryMarshal.GetReference(expected),
                ref MemoryMarshal.GetReference(actual)));

        public static void AreSameRef<T>(ReadOnlyMemory<T> expected, ReadOnlyMemory<T> actual) =>
            AreSameRef(expected.Span, actual.Span);
    }
}