﻿using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Text.Encoding;

namespace System.Net.Mqtt.PublishPacketTests
{
    [TestClass]
    public class PublishPacket_GetBytes_Should
    {
        private readonly PublishPacket samplePacket =
            new PublishPacket(0, default, "TestTopic", UTF8.GetBytes("TestMessage"));

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
            var bytes = new PublishPacket(0, default, "topic", default, duplicate: true).GetBytes().Span;

            var expectedDuplicateValue = PacketFlags.Duplicate;
            var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetDuplicateFlag_GivenMessageWith_Duplicate_False()
        {
            var bytes = new PublishPacket(0, default, "topic").GetBytes().Span;

            var expectedDuplicateValue = 0;
            var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
            Assert.AreEqual(expectedDuplicateValue, actualDuplicateValue);
        }

        [TestMethod]
        public void SetRetainFlag_GivenMessageWith_Retain_True()
        {
            var bytes = new PublishPacket(0, default, "topic", retain: true).GetBytes().Span;

            var expectedRetainValue = PacketFlags.Retain;
            var actualDuplicateValue = bytes[0] & PacketFlags.Retain;
            Assert.AreEqual(expectedRetainValue, actualDuplicateValue);
        }

        [TestMethod]
        public void ResetRetainFlag_GivenMessageWith_Retain_False()
        {
            var bytes = new PublishPacket(0, default, "topic").GetBytes().Span;

            var expectedRetainValue = 0;
            var actualRetainValue = bytes[0] & PacketFlags.Retain;
            Assert.AreEqual(expectedRetainValue, actualRetainValue);
        }

        [TestMethod]
        public void SetQoSFlag_0b00_GivenMessageWith_QoS_AtMostOnce()
        {
            var bytes = new PublishPacket(0, 0, "topic").GetBytes().Span;

            var expectedQoS = PacketFlags.QoSLevel0;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel0;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag_0b01_GivenMessageWith_QoS_AtLeastOnce()
        {
            var bytes = new PublishPacket(100, 1, "topic").GetBytes().Span;

            var expectedQoS = PacketFlags.QoSLevel1;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel1;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void SetQoSFlag_0b10_GivenMessageWith_QoS_ExactlyOnce()
        {
            var bytes = new PublishPacket(100, 2, "topic").GetBytes().Span;

            var expectedQoS = PacketFlags.QoSLevel2;
            var actualQoS = bytes[0] & PacketFlags.QoSLevel2;
            Assert.AreEqual(expectedQoS, actualQoS);
        }

        [TestMethod]
        public void EncodeTopic_TestTopic_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedTopicLength = 9;
            var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2));
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
            var bytes = new PublishPacket(100, 0, topic, payload).GetBytes().Span;

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

            var bytes = new PublishPacket(expectedPacketId, 1, topic, payload).GetBytes().Span;

            var expectedLength = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(expectedLength, actualLength);

            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(9));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodePacketId_GivenMessageWith_QoS_ExactlyOnce()
        {
            var topic = "topic";
            var payload = new byte[] {1, 1, 1, 1};
            ushort expectedPacketId = 100;

            var bytes = new PublishPacket(expectedPacketId, 2, topic, payload).GetBytes().Span;

            var expectedLength = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
            var actualLength = bytes.Length;
            Assert.AreEqual(expectedLength, actualLength);

            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(9));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }
    }
}