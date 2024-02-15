using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.BitSetIdentifierPool;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenNegativeBucketSize() => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(-1));

    [TestMethod]
    public void ThrowArgumentExceptionGivenZeroBucketSize() => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(0));

    [TestMethod]
    public void ThrowArgumentExceptionGivenNotPowerOf2BucketSize() => Assert.ThrowsException<ArgumentException>(() => new Mqtt.BitSetIdentifierPool(1000));

    [TestMethod]
    public void ThrowArgumentExceptionGivenLessThanMinBucketSize() => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(4));

    [TestMethod]
    public void NotThrowArgumentExceptionGivenPowerOf2BucketSize() => _ = new Mqtt.BitSetIdentifierPool(64);
}