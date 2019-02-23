using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.ExtensionsTests
{
    [TestClass]
    public class SpanExtensions_EncodeMqttLengthBytes_Should
    {
        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Throw_IndexOutOfRangeException_IfInsufficientBufferSizeProvided()
        {
            Span<byte> actualBytes = new byte[1];
            SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 2097151);
        }

        [TestMethod]
        public void Encode_1_Byte_0_GivenValueOf0()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 0);
            Assert.AreEqual(1, actualCount);
            Assert.AreEqual(0, actualBytes[0]);
            Assert.AreEqual(0, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_1_Byte_127_GivenValueOf127()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 127);
            Assert.AreEqual(1, actualCount);
            Assert.AreEqual(127, actualBytes[0]);
            Assert.AreEqual(0, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_2_Bytes_128_1_GivenValueOf128()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 128);
            Assert.AreEqual(2, actualCount);
            Assert.AreEqual(128, actualBytes[0]);
            Assert.AreEqual(1, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_2_Bytes_255_127_GivenValueOf16383()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 16383);
            Assert.AreEqual(2, actualCount);
            Assert.AreEqual(255, actualBytes[0]);
            Assert.AreEqual(127, actualBytes[1]);
            Assert.AreEqual(0, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_3_Bytes_128_128_1_GivenValueOf16384()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 16384);
            Assert.AreEqual(3, actualCount);
            Assert.AreEqual(128, actualBytes[0]);
            Assert.AreEqual(128, actualBytes[1]);
            Assert.AreEqual(1, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_3_Bytes_255_255_127_GivenValueOf2097151()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 2097151);
            Assert.AreEqual(3, actualCount);
            Assert.AreEqual(255, actualBytes[0]);
            Assert.AreEqual(255, actualBytes[1]);
            Assert.AreEqual(127, actualBytes[2]);
            Assert.AreEqual(0, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_4_Bytes_128_128_128_1_GivenValueOf2097152()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 2097152);
            Assert.AreEqual(4, actualCount);
            Assert.AreEqual(128, actualBytes[0]);
            Assert.AreEqual(128, actualBytes[1]);
            Assert.AreEqual(128, actualBytes[2]);
            Assert.AreEqual(1, actualBytes[3]);
        }

        [TestMethod]
        public void Encode_4_Bytes_255_255_255_127_GivenValueOf268435455()
        {
            Span<byte> actualBytes = new byte[4];
            var actualCount = SpanExtensions.EncodeMqttLengthBytes(ref actualBytes, 268435455);
            Assert.AreEqual(4, actualCount);
            Assert.AreEqual(255, actualBytes[0]);
            Assert.AreEqual(255, actualBytes[1]);
            Assert.AreEqual(255, actualBytes[2]);
            Assert.AreEqual(127, actualBytes[3]);
        }
    }
}