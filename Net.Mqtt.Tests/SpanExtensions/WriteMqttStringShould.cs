using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Net.Mqtt.Extensions.SpanExtensions;

namespace Net.Mqtt.Tests.SpanExtensions;

[TestClass]
public class WriteMqttStringShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionIfInsufficientBufferSizeProvided()
    {
        Span<byte> actualBytes = new byte[1];
        WriteMqttString(ref actualBytes, "abc"u8);
    }

    [TestMethod]
    public void EncodeAsValidUtf8BytesBigEndianWordSizePrefixedGivenAsciiString()
    {
        var actualBytes = new byte[5];
        var span = actualBytes.AsSpan();
        WriteMqttString(ref span, "abc"u8);
        Assert.IsTrue(span.IsEmpty);
        Assert.AreEqual(0, actualBytes[0]);
        Assert.AreEqual(3, actualBytes[1]);
        Assert.AreEqual(97, actualBytes[2]);
        Assert.AreEqual(98, actualBytes[3]);
        Assert.AreEqual(99, actualBytes[4]);
    }

    [TestMethod]
    public void EncodeAsValidUtf8BytesBigEndianWordSizePrefixedGivenUnicodeString()
    {
        var actualBytes = new byte[12];
        var span = actualBytes.AsSpan();
        WriteMqttString(ref span, "abc-абв"u8);
        Assert.IsTrue(span.IsEmpty);
        Assert.AreEqual(0, actualBytes[0]);
        Assert.AreEqual(10, actualBytes[1]);
        Assert.AreEqual(97, actualBytes[2]);
        Assert.AreEqual(98, actualBytes[3]);
        Assert.AreEqual(99, actualBytes[4]);
        Assert.AreEqual(45, actualBytes[5]);
        Assert.AreEqual(208, actualBytes[6]);
        Assert.AreEqual(176, actualBytes[7]);
        Assert.AreEqual(208, actualBytes[8]);
        Assert.AreEqual(177, actualBytes[9]);
        Assert.AreEqual(208, actualBytes[10]);
        Assert.AreEqual(178, actualBytes[11]);
    }
}