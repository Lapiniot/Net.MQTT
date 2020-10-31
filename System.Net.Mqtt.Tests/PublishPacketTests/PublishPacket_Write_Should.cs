using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests.PublishPacketTests
{
    [TestClass]
    public class PublishPacket_Write_Should
    {
        [TestMethod]
        public void SetHeaderBytes_48_24_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[24];
            new PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage")).Write(bytes, 22);

            const int expectedHeaderFlags = 0b0011_0000;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            const int expectedRemainingLength = 22;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void SetDuplicateFlag_GivenMessageWith_Duplicate_True()
        {
            var bytes = new byte[9];
            new PublishPacket(0, default, "topic", default, duplicate: true).Write(bytes, 7);

            const byte expectedDuplicateValue = PacketFlags.Duplicate;
            var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetDuplicateFlag_GivenMessageWith_Duplicate_False()
        {
            Span<byte> bytes = new byte[9];
            new PublishPacket(0, default, "topic").Write(bytes, 7);

            const int expectedDuplicateValue = 0;
            var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void SetRetainFlag_GivenMessageWith_Retain_True()
        {
            Span<byte> bytes = new byte[9];
            new PublishPacket(0, default, "topic", retain: true).Write(bytes, 7);

            const byte expectedRetainValue = PacketFlags.Retain;
            var actualDuplicateValue = bytes[0] & PacketFlags.Retain;
            Assert.AreEqual(expectedRetainValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetRetainFlag_GivenMessageWith_Retain_False()
        {
            Span<byte> bytes = new byte[9];
            new PublishPacket(0, default, "topic").Write(bytes, 7);

            const int expectedRetainValue = 0;
            var actualRetainValue = bytes[0] & PacketFlags.Retain;
            Assert.AreEqual(expectedRetainValue, actualRetainValue);
        }

        [TestMethod]
        public void SetQoSFlag_0b00_GivenMessageWith_QoS_AtMostOnce()
        {
            Span<byte> bytes = new byte[9];
            new PublishPacket(0, 0, "topic").Write(bytes, 7);

            const byte expectedQoS = PacketFlags.QoSLevel0;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel0;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag_0b01_GivenMessageWith_QoS_AtLeastOnce()
        {
            Span<byte> bytes = new byte[11];
            new PublishPacket(100, 1, "topic").Write(bytes, 9);

            const byte expectedQoS = PacketFlags.QoSLevel1;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel1;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag_0b10_GivenMessageWith_QoS_ExactlyOnce()
        {
            Span<byte> bytes = new byte[11];
            new PublishPacket(100, 2, "topic").Write(bytes, 9);

            const byte expectedQoS = PacketFlags.QoSLevel2;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel2;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void EncodeTopic_TestTopic_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[24];
            new PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage")).Write(bytes, 22);

            const int expectedTopicLength = 9;
            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            const string expectedTopic = "TestTopic";
            var actualTopic = UTF8.GetString(bytes.Slice(4, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);
        }

        [TestMethod]
        public void EncodePayload_TestMessage_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[24];
            new PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage")).Write(bytes, 22);

            const string expectedTopic = "TestMessage";
            var actualTopic = UTF8.GetString(bytes.Slice(bytes.Length - expectedTopic.Length, expectedTopic.Length));
            Assert.AreEqual(expectedTopic, actualTopic);
        }

        [TestMethod]
        public void NotEncodePacketId_GivenMessageWith_QoS_AtMostOnce()
        {
            const string topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};

            Span<byte> bytes = new byte[13];
            new PublishPacket(100, 0, topic, payload).Write(bytes, 11);

            var length = 1 + 1 + 2 + topic.Length + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(length, actualLength);
        }

        [TestMethod]
        public void EncodePacketId_GivenMessageWith_QoS_AtLeastOnce()
        {
            const string topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};
            const ushort packetId = 100;

            Span<byte> bytes = new byte[15];
            new PublishPacket(packetId, 1, topic, payload).Write(bytes, 13);

            var length = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(length, actualLength);

            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
            Assert.AreEqual(packetId, actualPacketId);
        }

        [TestMethod]
        public void EncodePacketId_GivenMessageWith_QoS_ExactlyOnce()
        {
            const string topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};
            const ushort packetId = 100;

            Span<byte> bytes = new byte[15];
            new PublishPacket(packetId, 2, topic, payload).Write(bytes, 13);

            var length = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(length, actualLength);

            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
            Assert.AreEqual(packetId, actualPacketId);
        }
    }
}