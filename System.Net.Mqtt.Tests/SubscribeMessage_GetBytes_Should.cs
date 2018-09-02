using System.Net.Mqtt.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.QoSLevel;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class SubscribeMessage_GetBytes_Should
    {
        private readonly SubscribeMessage sampleMessage = new SubscribeMessage(2)
        {
            Topics =
            {
                ("a/b/c", ExactlyOnce),
                ("d/e/f", AtLeastOnce),
                ("g/h/i", AtMostOnce)
            }
        };

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenPacketId0()
        {
            var _ = new SubscribeMessage(0);
        }

        [TestMethod]
        public void SetHeaderBytes_0x82_0x1a_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

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
            var bytes = sampleMessage.GetBytes().Span;

            byte expectedPacketId = 0x0002;
            var actualPacketId = ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeTopicsWithQoS_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedTopic = "a/b/c";
            var expectedTopicLength = expectedTopic.Length;
            var expectedQoS = ExactlyOnce;

            var actualTopicLength = ReadUInt16BigEndian(bytes.Slice(4));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            var actualTopic = UTF8.GetString(bytes.Slice(6, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            var actualQoS = (QoSLevel)bytes[11];
            Assert.AreEqual(expectedQoS, actualQoS);

            expectedTopic = "d/e/f";
            expectedTopicLength = expectedTopic.Length;
            expectedQoS = AtLeastOnce;

            actualTopicLength = ReadUInt16BigEndian(bytes.Slice(12));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = UTF8.GetString(bytes.Slice(14, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            actualQoS = (QoSLevel)bytes[19];
            Assert.AreEqual(expectedQoS, actualQoS);

            expectedTopic = "g/h/i";
            expectedTopicLength = expectedTopic.Length;
            expectedQoS = AtMostOnce;

            actualTopicLength = ReadUInt16BigEndian(bytes.Slice(20));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            actualTopic = UTF8.GetString(bytes.Slice(22, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);

            actualQoS = (QoSLevel)bytes[27];
            Assert.AreEqual(expectedQoS, actualQoS);
        }
    }
}