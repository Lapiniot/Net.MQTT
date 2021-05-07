using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ExtensionsTests
{
    [TestClass]
    public class SpanExtensionsEncodeMqttStringShould
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowArgumentOutOfRangeExceptionIfInsufficientBufferSizeProvided()
        {
            Span<byte> actualBytes = new byte[1];
            SpanExtensions.WriteMqttString(ref actualBytes, "abc");
        }

        [TestMethod]
        public void EncodeAsValidUtf8BytesBigEndianWordSizePrefixedGivenAsciiString()
        {
            Span<byte> actualBytes = new byte[5];
            var actualSize = SpanExtensions.WriteMqttString(ref actualBytes, "abc");
            Assert.AreEqual(5, actualSize);
            Assert.AreEqual(0, actualBytes[0]);
            Assert.AreEqual(3, actualBytes[1]);
            Assert.AreEqual(97, actualBytes[2]);
            Assert.AreEqual(98, actualBytes[3]);
            Assert.AreEqual(99, actualBytes[4]);
        }

        [TestMethod]
        public void EncodeAsValidUtf8BytesBigEndianWordSizePrefixedGivenUnicodeString()
        {
            Span<byte> actualBytes = new byte[12];
            var actualSize = SpanExtensions.WriteMqttString(ref actualBytes, "abc-абв");
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