using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SpanExtensions;

namespace System.Net.Mqtt.Tests.SpanExtensions
{
    [TestClass]
    public class WriteMqttLengthBytesShould
    {
        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void ThrowIndexOutOfRangeExceptionIfInsufficientBufferSizeProvided()
        {
            Span<byte> actualBytes = new byte[1];
            WriteMqttLengthBytes(ref actualBytes, 2097151);
        }

        [TestMethod]
        public void Encode1Byte0GivenValueOf0()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 0);
            Assert.AreEqual(1, actualCount);
            Assert.AreEqual(0, actualBytes[0]);
            Assert.AreEqual(0, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode1Byte127GivenValueOf127()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 127);
            Assert.AreEqual(1, actualCount);
            Assert.AreEqual(127, actualBytes[0]);
            Assert.AreEqual(0, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode2Bytes1281GivenValueOf128()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 128);
            Assert.AreEqual(2, actualCount);
            Assert.AreEqual(128, actualBytes[0]);
            Assert.AreEqual(1, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode2Bytes255127GivenValueOf16383()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 16383);
            Assert.AreEqual(2, actualCount);
            Assert.AreEqual(255, actualBytes[0]);
            Assert.AreEqual(127, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode3Bytes1281281GivenValueOf16384()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 16384);
            Assert.AreEqual(3, actualCount);
            Assert.AreEqual(128, actualBytes[0]);
            Assert.AreEqual(128, actualBytes[1]);
            Assert.AreEqual(1, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode3Bytes255255127GivenValueOf2097151()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 2097151);
            Assert.AreEqual(3, actualCount);
            Assert.AreEqual(255, actualBytes[0]);
            Assert.AreEqual(255, actualBytes[1]);
            Assert.AreEqual(127, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode4Bytes1281281281GivenValueOf2097152()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 2097152);
            Assert.AreEqual(4, actualCount);
            Assert.AreEqual(128, actualBytes[0]);
            Assert.AreEqual(128, actualBytes[1]);
            Assert.AreEqual(128, actualBytes[2]);
            Assert.AreEqual(1, actualBytes[3]);
        }

        [TestMethod]
        public void Encode4Bytes255255255127GivenValueOf268435455()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = WriteMqttLengthBytes(ref actualBytes, 268435455);
            Assert.AreEqual(4, actualCount);
            Assert.AreEqual(255, actualBytes[0]);
            Assert.AreEqual(255, actualBytes[1]);
            Assert.AreEqual(255, actualBytes[2]);
            Assert.AreEqual(127, actualBytes[3]);
        }
    }
}