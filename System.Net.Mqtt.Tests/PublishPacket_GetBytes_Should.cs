using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.QoSLevel;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class PublishPacket_GetBytes_Should
    {
        private readonly PublishPacket samplePacket = new PublishPacket("TestTopic", UTF8.GetBytes("TestMessage"))
        {
            Duplicate = false,
            Retain = false,
            QoSLevel = AtMostOnce
        };

        [TestMethod]
        public void SetHeaderBytes_48_24_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedHeaderFlags = (byte)PacketType.Publish;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            var expectedRemainingLength = 22;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void SetDuplicateFlag_GivenMessageWith_Duplicate_True()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {Duplicate = true}.GetBytes().Span;

            var expectedDuplicateValue = Duplicate;
            var actualDuplicateValue = bytes[0] & Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetDuplicateFlag_GivenMessageWith_Duplicate_False()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {Duplicate = false}.GetBytes().Span;

            var expectedDuplicateValue = 0;
            var actualDuplicateValue = bytes[0] & Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void SetRetainFlag_GivenMessageWith_Retain_True()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {Retain = true}.GetBytes().Span;

            var expectedRetainValue = Retain;
            var actualDuplicateValue = bytes[0] & Retain;
            Assert.AreEqual(expectedRetainValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetRetainFlag_GivenMessageWith_Retain_False()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {Retain = false}.GetBytes().Span;

            var expectedRetainValue = 0;
            var actualRetainValue = bytes[0] & Retain;
            Assert.AreEqual(expectedRetainValue, actualRetainValue);
        }

        [TestMethod]
        public void SetQoSFlag_0b00_GivenMessageWith_QoS_AtMostOnce()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {QoSLevel = AtMostOnce}.GetBytes().Span;

            var expectedQoS = QoSLevel0;
            var actualQoS = bytes[0] & QoSLevel0;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag_0b01_GivenMessageWith_QoS_AtLeastOnce()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {QoSLevel = AtLeastOnce}.GetBytes().Span;

            var expectedQoS = QoSLevel1;
            var actualQoS = bytes[0] & QoSLevel1;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag_0b10_GivenMessageWith_QoS_ExactlyOnce()
        {
            var bytes = new PublishPacket("topic", Memory<byte>.Empty) {QoSLevel = ExactlyOnce}.GetBytes().Span;

            var expectedQoS = QoSLevel2;
            var actualQoS = bytes[0] & QoSLevel2;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void EncodeTopic_TestTopic_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedTopicLength = 9;
            var actualTopicLength = ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            var expectedTopic = "TestTopic";
            var actualTopic = UTF8.GetString(bytes.Slice(4, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);
        }

        [TestMethod]
        public void EncodePayload_TestMessage_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedTopic = "TestMessage";
            var actualTopic = UTF8.GetString(bytes.Slice(bytes.Length - expectedTopic.Length, expectedTopic.Length));
            Assert.AreEqual(expectedTopic, actualTopic);
        }

        [TestMethod]
        public void NotEncodePacketId_GivenMessageWith_QoS_AtMostOnce()
        {
            var topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};
            var bytes = new PublishPacket(topic, payload) {PacketId = 100, QoSLevel = AtMostOnce}.GetBytes().Span;

            var expectedLength = 1 + 1 + 2 + topic.Length + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(expectedLength, actualLength);
        }

        [TestMethod]
        public void EncodePacketId_GivenMessageWith_QoS_AtLeastOnce()
        {
            var topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};
            ushort expectedPacketId = 100;

            var bytes = new PublishPacket(topic, payload) {PacketId = expectedPacketId, QoSLevel = AtLeastOnce}.GetBytes().Span;

            var expectedLength = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(expectedLength, actualLength);

            var actualPacketId = ReadUInt16BigEndian(bytes.Slice(9));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodePacketId_GivenMessageWith_QoS_ExactlyOnce()
        {
            var topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};
            ushort expectedPacketId = 100;

            var bytes = new PublishPacket(topic, payload) {PacketId = expectedPacketId, QoSLevel = ExactlyOnce}.GetBytes().Span;

            var expectedLength = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(expectedLength, actualLength);

            var actualPacketId = ReadUInt16BigEndian(bytes.Slice(9));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }
    }
}