using System.Memory;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace System.Net.Mqtt.PublishPacketTests
{
    [TestClass]
    public class PublishPacket_TryParse_Should
    {
        private readonly ByteSequence sampleComplete = new ByteSequence(
            new byte[]
            {
                0x3b, 0x0e, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04, 0x03,
                0x04, 0x05, 0x04, 0x03
            });

        private readonly ByteSequence sampleDuplicateFlag = new ByteSequence(
            new byte[]
            {
                0x38, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63
            });


        private readonly ByteSequence sampleFragmented;

        private readonly ByteSequence sampleIncomplete = new ByteSequence(
            new byte[]
            {
                0x3b, 0x0e, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04, 0x03
            });

        private readonly ByteSequence sampleNoFlags = new ByteSequence(
            new byte[]
            {
                0x30, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63
            });

        private readonly ByteSequence sampleQosAtLeastOnce = new ByteSequence(
            new byte[]
            {
                0x32, 0x09, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04
            });

        private readonly ByteSequence sampleQosAtMostOnce = new ByteSequence(
            new byte[]
            {
                0x30, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04
            });

        private readonly ByteSequence sampleQosExactlyOnce = new ByteSequence(
            new byte[]
            {
                0x34, 0x09, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04
            });

        private readonly ByteSequence sampleRetainFlag = new ByteSequence(
            new byte[]
            {
                0x31, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63
            });

        public PublishPacket_TryParse_Should()
        {
            var segment1 = new Segment<byte>(new byte[]
            {
                0x3b, 0x0e, 0x00, 0x05
            });

            var segment2 = segment1.Append(new byte[]
            {
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04, 0x03,
                0x04, 0x05, 0x04, 0x03
            });

            sampleFragmented = new ByteSequence(segment1, 0, segment2, 12);
        }

        [TestMethod]
        public void ReturnQoSLevel_AtMostOnce_GivenSampleWithQoS0()
        {
            var actualResult = PublishPacket.TryParse(sampleQosAtMostOnce, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual(0, p.QoSLevel);
        }

        [TestMethod]
        public void ReturnQoSLevel_AtLeastOnce_GivenSampleWithQoS1()
        {
            var actualResult = PublishPacket.TryParse(sampleQosAtLeastOnce, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual(1, p.QoSLevel);
        }

        [TestMethod]
        public void ReturnQoSLevel_ExactlyOnce_GivenSampleWithQoS2()
        {
            var actualResult = PublishPacket.TryParse(sampleQosExactlyOnce, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual(2, p.QoSLevel);
        }

        [TestMethod]
        public void ReturnDuplicateTrue_GivenSampleWithDupFlag1()
        {
            var actualResult = PublishPacket.TryParse(sampleDuplicateFlag, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.IsTrue(p.Duplicate);
        }

        [TestMethod]
        public void ReturnDuplicateFalse_GivenSampleWithDupFlag0()
        {
            var actualResult = PublishPacket.TryParse(sampleNoFlags, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.IsFalse(p.Duplicate);
        }

        [TestMethod]
        public void ReturnRetainTrue_GivenSampleWithRetainFlag1()
        {
            var actualResult = PublishPacket.TryParse(sampleRetainFlag, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.IsTrue(p.Retain);
        }

        [TestMethod]
        public void ReturnRetainFalse_GivenSampleWithRetainFlag0()
        {
            var actualResult = PublishPacket.TryParse(sampleNoFlags, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.IsFalse(p.Retain);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_Consumed16_GivenValidSample()
        {
            var actualResult = PublishPacket.TryParse(sampleComplete, out var packet, out var consumed);

            Assert.IsTrue(actualResult);
            Assert.IsNotNull(packet);
            Assert.AreEqual(16, consumed);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_Consumed0_GivenSampleIncomplete()
        {
            var actualResult = PublishPacket.TryParse(sampleIncomplete, out var packet, out var consumed);

            Assert.IsFalse(actualResult);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }

        [TestMethod]
        public void NotDecodePacketId_GivenSampleQoS_0()
        {
            var actualResult = PublishPacket.TryParse(sampleQosAtMostOnce, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual(0x00, p.Id);
        }

        [TestMethod]
        public void DecodePacketId_0x04_GivenSampleQoS_1()
        {
            var actualResult = PublishPacket.TryParse(sampleQosAtLeastOnce, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual(0x04, p.Id);
        }

        [TestMethod]
        public void DecodePacketId_0x04_GivenSampleQoS_2()
        {
            var actualResult = PublishPacket.TryParse(sampleQosExactlyOnce, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual(0x04, p.Id);
        }

        [TestMethod]
        public void DecodeTopic_abc_GivenSample()
        {
            var actualResult = PublishPacket.TryParse(sampleComplete, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual("a/b/c", p.Topic);
        }

        [TestMethod]
        public void DecodeTopic_abc_GivenSampleFragmented()
        {
            var actualResult = PublishPacket.TryParse(sampleFragmented, out var p, out _);

            Assert.IsTrue(actualResult);

            Assert.AreEqual("a/b/c", p.Topic);
        }

        [TestMethod]
        public void DecodePayload_0x03_0x04_0x05_0x04_0x03_GivenSample()
        {
            var actualResult = PublishPacket.TryParse(sampleComplete, out var p, out _);

            Assert.IsTrue(actualResult);

            var span = p.Payload.Span;

            Assert.AreEqual(5, span.Length);

            Assert.AreEqual(0x03, span[0]);
            Assert.AreEqual(0x04, span[1]);
            Assert.AreEqual(0x05, span[2]);
            Assert.AreEqual(0x04, span[3]);
            Assert.AreEqual(0x03, span[4]);
        }

        [TestMethod]
        public void DecodePayload_0x03_0x04_0x05_0x04_0x03_GivenSampleFragmented()
        {
            var actualResult = PublishPacket.TryParse(sampleFragmented, out var p, out _);

            Assert.IsTrue(actualResult);

            var span = p.Payload.Span;

            Assert.AreEqual(5, span.Length);

            Assert.AreEqual(0x03, span[0]);
            Assert.AreEqual(0x04, span[1]);
            Assert.AreEqual(0x05, span[2]);
            Assert.AreEqual(0x04, span[3]);
            Assert.AreEqual(0x03, span[4]);
        }
    }
}