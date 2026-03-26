using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable IDE0051 // Remove unused private member

namespace Net.Mqtt.Server.Tests;

internal static class AssertExtensions
{
    extension(CollectionAssert)
    {
        [StackTraceHidden]
        public static void AreEqual<T>(ImmutableArray<T> expected, ImmutableArray<T> actual)
        {
            AreEqual(expected.AsSpan(), actual.AsSpan());
        }

        [StackTraceHidden]
        public static void AreEqual<T>(ReadOnlyMemory<T> expected, ReadOnlyMemory<T> actual)
        {
            AreEqual(expected.Span, actual.Span);
        }

        [StackTraceHidden]
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