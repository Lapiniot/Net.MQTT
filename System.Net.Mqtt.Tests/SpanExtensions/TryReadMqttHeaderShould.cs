using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SpanExtensions;

namespace System.Net.Mqtt.Tests.SpanExtensions;

[TestClass]
public class TryReadMqttHeaderShould
{
    [TestMethod]
    public void ReturnFalseGivenEmptySample()
    {
        var emptySample = ReadOnlySpan<byte>.Empty;

        var actual = TryReadMqttHeader(in emptySample, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenIncompleteSample()
    {
        ReadOnlySpan<byte> incompleteSample = stackalloc byte[] { 64, 205, 255, 255 };

        var actual = TryReadMqttHeader(in incompleteSample, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenBadSample()
    {
        ReadOnlySpan<byte> badSample = stackalloc byte[] { 64, 205, 255, 255, 255, 127, 0 };

        var actual = TryReadMqttHeader(in badSample, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSample()
    {
        ReadOnlySpan<byte> completeSample = stackalloc byte[] { 64, 205, 255, 255, 127, 0, 0 };

        var actual = TryReadMqttHeader(in completeSample, out _, out _, out _);

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnPacketFlags64GivenCompleteSample()
    {
        const int expectedFlags = 64;

        ReadOnlySpan<byte> completeSample = stackalloc byte[] { 64, 205, 255, 255, 127, 0, 0 };

        TryReadMqttHeader(in completeSample, out var actualFlags, out _, out _);

        Assert.AreEqual(expectedFlags, actualFlags);
    }

    [TestMethod]
    public void ReturnLength268435405GivenCompleteSample()
    {
        const int expectedLength = 268435405;

        ReadOnlySpan<byte> completeSample = stackalloc byte[] { 64, 205, 255, 255, 127, 0, 0 };

        TryReadMqttHeader(in completeSample, out _, out var actualLength, out _);

        Assert.AreEqual(expectedLength, actualLength);
    }

    [TestMethod]
    public void ReturnDataOffset5GivenCompleteSample()
    {
        const int expectedDataOffset = 5;

        ReadOnlySpan<byte> completeSample = stackalloc byte[] { 64, 205, 255, 255, 127, 0, 0 };

        TryReadMqttHeader(in completeSample, out _, out _, out var actualDataOffset);

        Assert.AreEqual(expectedDataOffset, actualDataOffset);
    }
}