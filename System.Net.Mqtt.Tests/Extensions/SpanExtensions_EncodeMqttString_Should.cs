using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Extensions
{
    [TestClass]
    public class SpanExtensions_EncodeMqttString_Should
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Throw_ArgumentOutOfRangeException_IfInsufficientBufferSizeProvided()
        {
            Span<byte> actualBytes = new byte[1];
            SpanExtensions.EncodeMqttString("abc", ref actualBytes);
        }

        [TestMethod]
        public void Encode_AsValidUtf8Bytes_BigEndianWordSizePrefixed_GivenAsciiString()
        {
            Span<byte> actualBytes = new byte[5];
            var actualSize = SpanExtensions.EncodeMqttString("abc", ref actualBytes);
            Assert.AreEqual(5, actualSize);
            Assert.AreEqual(0, actualBytes[0]);
            Assert.AreEqual(3, actualBytes[1]);
            Assert.AreEqual(97, actualBytes[2]);
            Assert.AreEqual(98, actualBytes[3]);
            Assert.AreEqual(99, actualBytes[4]);
        }

        [TestMethod]
        public void Encode_AsValidUtf8Bytes_BigEndianWordSizePrefixed_GivenUnicodeString()
        {
            Span<byte> actualBytes = new byte[12];
            var actualSize = SpanExtensions.EncodeMqttString("abc-абв", ref actualBytes);
            Assert.AreEqual(12, actualSize);
            Assert.AreEqual(0, actualBytes[0]);
            Assert.AreEqual(10, actualBytes[1]);
            Assert.AreEqual(97, actualBytes[2]);
            Assert.AreEqual(98, actualBytes[3]);
            Assert.AreEqual(99, actualBytes[4]);
            Assert.AreEqual(45, actualBytes[5]);
            Assert.AreEqual(208, actualBytes[6]);
            Assert.AreEqual(176, actualBytes[7]);
            Assert.AreEqual(208, actualBytes[8]);
            Assert.AreEqual(177, actualBytes[9]);
            Assert.AreEqual(208, actualBytes[10]);
            Assert.AreEqual(178, actualBytes[11]);
        }
    }
}