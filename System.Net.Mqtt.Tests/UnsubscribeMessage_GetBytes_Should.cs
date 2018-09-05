using System.Net.Mqtt.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class UnsubscribeMessage_GetBytes_Should
    {
        private readonly UnsubscribeMessage sampleMessage = new UnsubscribeMessage(2)
        {
            Topics = {"a/b/c", "d/e/f", "g/h/i"}
        };

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenPacketId0()
        {
            var _ = new UnsubscribeMessage(0);
        }

        [TestMethod]
        public void SetHeaderBytes_0x82_0x17_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

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
            var bytes = sampleMessage.GetBytes().Span;

            byte expectedPacketId = 0x0002;
            var actualPacketId = ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeTopics_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedTopic = "a/b/c";
            var expectedTopicLength = expectedTopic.Length;

            var actualTopicLength = ReadUInt16BigEndian(bytes.Slice(4));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            var actualTopic = UTF8.GetString(bytes.Slice(6, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            expectedTopic = "d/e/f";
            expectedTopicLength = expectedTopic.Length;

            actualTopicLength = ReadUInt16BigEndian(bytes.Slice(11));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = UTF8.GetString(bytes.Slice(13, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            expectedTopic = "g/h/i";
            expectedTopicLength = expectedTopic.Length;

            actualTopicLength = ReadUInt16BigEndian(bytes.Slice(18));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = UTF8.GetString(bytes.Slice(20, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);
        }
    }
}