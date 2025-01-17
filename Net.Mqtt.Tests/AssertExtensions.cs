using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Net.Mqtt.Tests;

public static class AssertExtensions
{
    public static void AreSameRef<T>(this Assert that, ReadOnlySpan<T> expected, ReadOnlySpan<T> actual) =>
        Assert.IsTrue(Unsafe.AreSame(
            ref MemoryMarshal.GetReference(expected),
            ref MemoryMarshal.GetReference(actual)));

    public static void AreSameRef<T>(this Assert that, ReadOnlyMemory<T> expected, ReadOnlyMemory<T> actual) =>
        AreSameRef(that, expected.Span, actual.Span);
}