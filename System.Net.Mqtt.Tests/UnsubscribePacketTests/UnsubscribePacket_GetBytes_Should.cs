using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.UnsubscribePacketTests
{
    [TestClass]
    public class UnsubscribePacket_GetBytes_Should
    {
        private readonly UnsubscribePacket samplePacket = new UnsubscribePacket(2)
        {
            Topics = {"a/b/c", "d/e/f", "g/h/i"}
        };

        [TestMethod]
        public void SetHeaderBytes_0xa2_0x17_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            byte expectedHeaderFlags = (byte)PacketType.Unsubscribe | 0b0010;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            var expectedRemainingLength = 0x17;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void EncodePacketId_0x0002_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            byte expectedPacketId = 0x0002;
            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeTopics_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedTopic = "a/b/c";
            var expectedTopicLength = expectedTopic.Length;

            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(4));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            var actualTopic = Encoding.UTF8.GetString(bytes.Slice(6, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            expectedTopic = "d/e/f";
            expectedTopicLength = expectedTopic.Length;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(11));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(13, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            expectedTopic = "g/h/i";
            expectedTopicLength = expectedTopic.Length;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(18));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(20, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);
        }
    }
}