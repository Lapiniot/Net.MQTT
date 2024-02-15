using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Net.Mqtt.Extensions.SpanExtensions;

namespace Net.Mqtt.Tests.SpanExtensions;

[TestClass]
public class WriteMqttVarByteIntegerShould
{
    [TestMethod]
    [ExpectedException(typeof(IndexOutOfRangeException))]
    public void ThrowIndexOutOfRangeExceptionIfInsufficientBufferSizeProvided()
    {
        Span<byte> actualBytes = new byte[1];
        WriteMqttVarByteInteger(ref actualBytes, 2097151);
    }

    [TestMethod]
    public void Encode1Byte0GivenValueOf0()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 0);
        Assert.AreEqual(3, span.Length);
        Assert.AreEqual(0, actualBytes[0]);
        Assert.AreEqual(0, actualBytes[1]);
        Assert.AreEqual(0, actualBytes[2]);
        Assert.AreEqual(0, actualBytes[3]);
    }

    [TestMethod]
    public void Encode1Byte127GivenValueOf127()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 127);
        Assert.AreEqual(3, span.Length);
        Assert.AreEqual(127, actualBytes[0]);
        Assert.AreEqual(0, actualBytes[1]);
        Assert.AreEqual(0, actualBytes[2]);
        Assert.AreEqual(0, actualBytes[3]);
    }

    [TestMethod]
    public void Encode2Bytes1281GivenValueOf128()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 128);
        Assert.AreEqual(span.Length, 2);
        Assert.AreEqual(128, actualBytes[0]);
        Assert.AreEqual(1, actualBytes[1]);
        Assert.AreEqual(0, actualBytes[2]);
        Assert.AreEqual(0, actualBytes[3]);
    }

    [TestMethod]
    public void Encode2Bytes255127GivenValueOf16383()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 16383);
        Assert.AreEqual(span.Length, 2);
        Assert.AreEqual(255, actualBytes[0]);
        Assert.AreEqual(127, actualBytes[1]);
        Assert.AreEqual(0, actualBytes[2]);
        Assert.AreEqual(0, actualBytes[3]);
    }

    [TestMethod]
    public void Encode3Bytes1281281GivenValueOf16384()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 16384);
        Assert.AreEqual(span.Length, 1);
        Assert.AreEqual(128, actualBytes[0]);
        Assert.AreEqual(128, actualBytes[1]);
        Assert.AreEqual(1, actualBytes[2]);
        Assert.AreEqual(0, actualBytes[3]);
    }

    [TestMethod]
    public void Encode3Bytes255255127GivenValueOf2097151()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 2097151);
        Assert.AreEqual(span.Length, 1);
        Assert.AreEqual(255, actualBytes[0]);
        Assert.AreEqual(255, actualBytes[1]);
        Assert.AreEqual(127, actualBytes[2]);
        Assert.AreEqual(0, actualBytes[3]);
    }

    [TestMethod]
    public void Encode4Bytes1281281281GivenValueOf2097152()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 2097152);
        Assert.IsTrue(span.IsEmpty);
        Assert.AreEqual(128, actualBytes[0]);
        Assert.AreEqual(128, actualBytes[1]);
        Assert.AreEqual(128, actualBytes[2]);
        Assert.AreEqual(1, actualBytes[3]);
    }

    [TestMethod]
    public void Encode4Bytes255255255127GivenValueOf268435455()
    {
        var actualBytes = new byte[4];
        var span = actualBytes.AsSpan();
        WriteMqttVarByteInteger(ref span, 268435455);
        Assert.IsTrue(span.IsEmpty);
        Assert.AreEqual(255, actualBytes[0]);
        Assert.AreEqual(255, actualBytes[1]);
        Assert.AreEqual(255, actualBytes[2]);
        Assert.AreEqual(127, actualBytes[3]);
    }
}