using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests.PublishPacket
{
    [TestClass]
    public class WriteShould
    {
        [TestMethod]
        public void SetHeaderBytes4824GivenSampleMessage()
        {
            Span<byte> bytes = new byte[24];
            new Packets.PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage")).Write(bytes, 22);

            const int expectedHeaderFlags = 0b0011_0000;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            const int expectedRemainingLength = 22;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void SetDuplicateFlagGivenMessageWithDuplicateTrue()
        {
            var bytes = new byte[9];
            new Packets.PublishPacket(0, default, "topic", default, duplicate: true).Write(bytes, 7);

            const byte expectedDuplicateValue = PacketFlags.Duplicate;
            var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetDuplicateFlagGivenMessageWithDuplicateFalse()
        {
            Span<byte> bytes = new byte[9];
            new Packets.PublishPacket(0, default, "topic").Write(bytes, 7);

            const int expectedDuplicateValue = 0;
            var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void SetRetainFlagGivenMessageWithRetainTrue()
        {
            Span<byte> bytes = new byte[9];
            new Packets.PublishPacket(0, default, "topic", retain: true).Write(bytes, 7);

            const byte expectedRetainValue = PacketFlags.Retain;
            var actualDuplicateValue = bytes[0] & PacketFlags.Retain;
            Assert.AreEqual(expectedRetainValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetRetainFlagGivenMessageWithRetainFalse()
        {
            Span<byte> bytes = new byte[9];
            new Packets.PublishPacket(0, default, "topic").Write(bytes, 7);

            const int expectedRetainValue = 0;
            var actualRetainValue = bytes[0] & PacketFlags.Retain;
            Assert.AreEqual(expectedRetainValue, actualRetainValue);
        }

        [TestMethod]
        public void SetQoSFlag0b00GivenMessageWithQoSAtMostOnce()
        {
            Span<byte> bytes = new byte[9];
            new Packets.PublishPacket(0, 0, "topic").Write(bytes, 7);

            const byte expectedQoS = PacketFlags.QoSLevel0;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel0;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag0b01GivenMessageWithQoSAtLeastOnce()
        {
            Span<byte> bytes = new byte[11];
            new Packets.PublishPacket(100, 1, "topic").Write(bytes, 9);

            const byte expectedQoS = PacketFlags.QoSLevel1;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel1;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag0b10GivenMessageWithQoSExactlyOnce()
        {
            Span<byte> bytes = new byte[11];
            new Packets.PublishPacket(100, 2, "topic").Write(bytes, 9);

            const byte expectedQoS = PacketFlags.QoSLevel2;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel2;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void EncodeTopicTestTopicGivenSampleMessage()
        {
            Span<byte> bytes = new byte[24];
            new Packets.PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage")).Write(bytes, 22);

            const int expectedTopicLength = 9;
            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            Assert.AreEqual(expectedTopicLength, actualTopicLength);

            const string expectedTopic = "TestTopic";
            var actualTopic = UTF8.GetString(bytes.Slice(4, expectedTopicLength));
            Assert.AreEqual(expectedTopic, actualTopic);
        }

        [TestMethod]
        public void EncodePayloadTestMessageGivenSampleMessage()
        {
            Span<byte> bytes = new byte[24];
            new Packets.PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage")).Write(bytes, 22);

            const string expectedTopic = "TestMessage";
            var actualTopic = UTF8.GetString(bytes.Slice(bytes.Length - expectedTopic.Length, expectedTopic.Length));
            Assert.AreEqual(expectedTopic, actualTopic);
        }

        [TestMethod]
        public void NotEncodePacketIdGivenMessageWithQoSAtMostOnce()
        {
            const string topic = "topic";
            var payload = new byte[] { 1, 1, 1, 1 };

            Span<byte> bytes = new byte[13];
            new Packets.PublishPacket(100, 0, topic, payload).Write(bytes, 11);

            var length = 1 + 1 + 2 + topic.Length + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(length, actualLength);
        }

        [TestMethod]
        public void EncodePacketIdGivenMessageWithQoSAtLeastOnce()
        {
            const string topic = "topic";
            var payload = new byte[] { 1, 1, 1, 1 };
            const ushort packetId = 100;

            Span<byte> bytes = new byte[15];
            new Packets.PublishPacket(packetId, 1, topic, payload).Write(bytes, 13);

            var length = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(length, actualLength);

            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
            Assert.AreEqual(packetId, actualPacketId);
        }

        [TestMethod]
        public void EncodePacketIdGivenMessageWithQoSExactlyOnce()
        {
            const string topic = "topic";
            var payload = new byte[] { 1, 1, 1, 1 };
            const ushort packetId = 100;

            Span<byte> bytes = new byte[15];
            new Packets.PublishPacket(packetId, 2, topic, payload).Write(bytes, 13);

            var length = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(length, actualLength);

            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
            Assert.AreEqual(packetId, actualPacketId);
        }
    }
}