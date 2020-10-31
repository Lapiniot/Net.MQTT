using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacketTests
{
    [TestClass]
    public class UnsubscribePacket_Write_Should
    {
        private readonly UnsubscribePacket samplePacket = new UnsubscribePacket(2, "a/b/c", "d/e/f", "g/h/i");

        [TestMethod]
        public void SetHeaderBytes_0xa2_0x17_GivenSampleMessage()
        {
            var bytes = new byte[25];
            samplePacket.Write(bytes, 23);

            const byte expectedHeaderFlags = 0b1010_0000 | 0b0010;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            const int expectedRemainingLength = 23;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void EncodePacketId_0x0002_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[25];
            samplePacket.Write(bytes, 23);

            const byte expectedPacketId = 0x0002;
            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeTopics_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[25];
            samplePacket.Write(bytes, 23);

            var topic = "a/b/c";
            var topicLength = topic.Length;

            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]);
            Assert.AreEqual(topicLength, actualTopicLength);

            var actualTopic = Encoding.UTF8.GetString(bytes.Slice(6, topicLength));
            Assert.AreEqual(topic, actualTopic);

            topic = "d/e/f";
            topicLength = topic.Length;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[11..]);
            Assert.AreEqual(topicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(13, topicLength));
            Assert.AreEqual(topic, actualTopic);

            topic = "g/h/i";
            topicLength = topic.Length;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[18..]);
            Assert.AreEqual(topicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(20, topicLength));
            Assert.AreEqual(topic, actualTopic);
        }
    }
}