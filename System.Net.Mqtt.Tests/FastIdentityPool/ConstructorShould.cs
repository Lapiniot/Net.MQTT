using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.FastIdentityPool;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenNegativeBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastIdentityPool(-1));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenZeroBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastIdentityPool(0));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenNotPowerOf2BucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastIdentityPool(1000));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenPowerOf2LessThanMinBucketSize()
    {
        Assert.ThrowsException<ArgumentException>(() => new Mqtt.FastIdentityPool(4));
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenPowerOf2BucketSize()
    {
        _ = new Mqtt.FastIdentityPool(64);
    }
}