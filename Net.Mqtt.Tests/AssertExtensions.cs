using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

#pragma warning disable CA1034 // Nested types should not be visible

namespace Net.Mqtt.Tests;

internal static class AssertExtensions
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

    extension(CollectionAssert)
    {
        public static void AreEqual<T>(ReadOnlyMemory<T> expected, ReadOnlyMemory<T> actual, string? message = "")
        {
            Assert.IsTrue(actual.Span.SequenceEqual(expected.Span), message);
        }

        public static void AreEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, string? message = "")
        {
            Assert.IsTrue(actual.SequenceEqual(expected), message);
        }
    }
}