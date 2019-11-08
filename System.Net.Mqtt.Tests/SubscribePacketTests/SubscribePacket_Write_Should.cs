using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubscribePacketTests
{
    [TestClass]
    public class SubscribePacket_Write_Should
    {
        private readonly SubscribePacket samplePacket = new SubscribePacket(2,
            ("a/b/c", 2), ("d/e/f", 1), ("g/h/i", 0));

        [TestMethod]
        public void SetHeaderBytes_0x82_0x1a_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[28];
            samplePacket.Write(bytes, 26);

            byte expectedHeaderFlags = 0b1000_0000 | 0b0010;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            var expectedRemainingLength = 0x1a;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void EncodePacketId_0x0002_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[28];
            samplePacket.Write(bytes, 26);

            byte expectedPacketId = 0x0002;
            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeTopicsWithQoS_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[28];
            samplePacket.Write(bytes, 26);

            var expectedTopic = "a/b/c";
            var expectedTopicLength = expectedTopic.Length;
            var expectedQoS = 2;

            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(4));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            var actualTopic = Encoding.UTF8.GetString(bytes.Slice(6, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            var actualQoS = bytes[11];
            Assert.AreEqual(expectedQoS, actualQoS);

            expectedTopic = "d/e/f";
            expectedTopicLength = expectedTopic.Length;
            expectedQoS = 1;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(12));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(14, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            actualQoS = bytes[19];
            Assert.AreEqual(expectedQoS, actualQoS);

            expectedTopic = "g/h/i";
            expectedTopicLength = expectedTopic.Length;
            expectedQoS = 0;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(20));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(22, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            actualQoS = bytes[27];
            Assert.AreEqual(expectedQoS, actualQoS);
        }
    }
}