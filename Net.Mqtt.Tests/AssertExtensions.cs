using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable IDE0051 // Remove unused private member

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
        public static void AreEqual<T>(ReadOnlyMemory<T> expected, ReadOnlyMemory<T> actual)
        {
            AreEqual(expected.Span, actual.Span);
        }

        public static void AreEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual)
        {
            if (actual.Length != expected.Length)
            {
                ThrowAssertFailed("CollectionAssert.AreEqual", "Different number of elements.");
            }

            var index = expected.CommonPrefixLength(actual);

            if (index != expected.Length)
            {
                ThrowAssertFailed("CollectionAssert.AreEqual", $"""
                Elements at index {index} do not match.
                Expected: {expected[index]}
                Actual: {actual[index]}
                """);
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowAssertFailed(string assertionName, string message) =>
            throw new AssertFailedException($"{assertionName} failed. {message}");
    }
}