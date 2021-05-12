using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.FastPacketIdPoolTests
{
    [TestClass]
    public class FastPacketIdPoolConstructorShould
    {
        [TestMethod]
        public void ThrowArgumentExceptionGivenNegativeBucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(-1));
        }

        [TestMethod]
        public void ThrowArgumentExceptionGivenZeroBucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(0));
        }

        [TestMethod]
        public void ThrowArgumentExceptionGivenNotPowerOf2BucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(1000));
        }

        [TestMethod]
        public void ThrowArgumentExceptionGivenPowerOf2LessThanDefaultBucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(16));
        }

        [TestMethod]
        public void NotThrowArgumentExceptionGivenPowerOf2BucketSize()
        {
            var unused = new FastPacketIdPool(64);
        }
    }
}