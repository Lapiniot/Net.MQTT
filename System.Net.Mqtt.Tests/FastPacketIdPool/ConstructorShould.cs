using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.FastPacketIdPool;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenNegativeBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastPacketIdPool(-1));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenZeroBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastPacketIdPool(0));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenNotPowerOf2BucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastPacketIdPool(1000));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenPowerOf2LessThanDefaultBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastPacketIdPool(16));
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenPowerOf2BucketSize()
    {
        var unused = new Mqtt.FastPacketIdPool(64);
    }
}