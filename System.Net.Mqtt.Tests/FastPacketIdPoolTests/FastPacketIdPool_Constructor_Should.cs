using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.FastPacketIdPoolTests
{
    [TestClass]
    public class FastPacketIdPool_Constructor_Should
    {
        [TestMethod]
        public void Throw_ArgumentException_GivenNegativeBucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(-1));
        }

        [TestMethod]
        public void Throw_ArgumentException_GivenZeroBucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(0));
        }

        [TestMethod]
        public void Throw_ArgumentException_GivenNotPowerOf2BucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(1000));
        }

        [TestMethod]
        public void Throw_ArgumentException_GivenPowerOf2LessThanDefaultBucketSize()
        {
            Assert.ThrowsException<ArgumentException>(() => new FastPacketIdPool(16));
        }

        [TestMethod]
        public void NotThrow_ArgumentException_GivenPowerOf2BucketSize()
        {
            _ = new FastPacketIdPool(64);
        }
    }
}