namespace Net.Mqtt.Tests.BitSetIdentifierPool;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenNegativeBucketSize() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(-1));

    [TestMethod]
    public void ThrowArgumentExceptionGivenZeroBucketSize() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(0));

    [TestMethod]
    public void ThrowArgumentExceptionGivenNotPowerOf2BucketSize() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(1000));

    [TestMethod]
    public void ThrowArgumentExceptionGivenLessThanMinBucketSize() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Mqtt.BitSetIdentifierPool(4));

    [TestMethod]
    public void NotThrowArgumentExceptionGivenPowerOf2BucketSize() => _ = new Mqtt.BitSetIdentifierPool(64);
}