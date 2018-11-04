using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.SubscribePacketTests
{
    [TestClass]
    public class SubscribePacket_GetBytes_Should
    {
        private readonly SubscribePacket samplePacket = new SubscribePacket(2,
            ("a/b/c", ExactlyOnce), ("d/e/f", AtLeastOnce), ("g/h/i", AtMostOnce));

        [TestMethod]
        public void SetHeaderBytes_0x82_0x1a_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            byte expectedHeaderFlags = (byte)PacketType.Subscribe | 0b0010;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            var expectedRemainingLength = 0x1a;
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
        public void EncodeTopicsWithQoS_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedTopic = "a/b/c";
            var expectedTopicLength = expectedTopic.Length;
            var expectedQoS = ExactlyOnce;

            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(4));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            var actualTopic = Encoding.UTF8.GetString(bytes.Slice(6, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            var actualQoS = (QoSLevel)bytes[11];
            Assert.AreEqual(expectedQoS, actualQoS);

            expectedTopic = "d/e/f";
            expectedTopicLength = expectedTopic.Length;
            expectedQoS = AtLeastOnce;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(12));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(14, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            actualQoS = (QoSLevel)bytes[19];
            Assert.AreEqual(expectedQoS, actualQoS);

            expectedTopic = "g/h/i";
            expectedTopicLength = expectedTopic.Length;
            expectedQoS = AtMostOnce;

            actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(20));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = Encoding.UTF8.GetString(bytes.Slice(22, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            actualQoS = (QoSLevel)bytes[27];
            Assert.AreEqual(expectedQoS, actualQoS);
        }
    }
}