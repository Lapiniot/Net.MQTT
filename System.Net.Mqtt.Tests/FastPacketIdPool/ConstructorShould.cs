using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.FastPacketIdPool;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenNegativeBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new FastIdentityPool(-1));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenZeroBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new FastIdentityPool(0));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenNotPowerOf2BucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new FastIdentityPool(1000));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenPowerOf2LessThanDefaultBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new FastIdentityPool(16));
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenPowerOf2BucketSize()
    {
        _ = new FastIdentityPool(64);
    }
}