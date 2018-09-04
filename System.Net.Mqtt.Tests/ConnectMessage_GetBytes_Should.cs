﻿using System.Net.Mqtt.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class ConnectMessage_GetBytes_Should
    {
        private readonly ConnectMessage sampleMessage = new ConnectMessage("TestClientId")
        {
            KeepAlive = 120,
            ProtocolName = "MQTT",
            ProtocolLevel = 0x04,
            UserName = "TestUser",
            Password = "TestPassword",
            WillTopic = "TestWillTopic",
            WillMessage = UTF8.GetBytes("TestWillMessage")
        };

        [TestMethod]
        public void SetHeaderBytes_16_80_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedPacketType = (byte)PacketType.Connect;
            var actualPacketType = bytes[0];
            Assert.AreEqual(expectedPacketType, actualPacketType);

            var expectedRemainingLength = 80;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void SetProtocolInfoBytes__GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedProtocolNameLength = 4;
            var actualProtocolNameLength = ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedProtocolNameLength, actualProtocolNameLength);

            var expectedProtocolName = "MQTT";
            var actualProtocolName = UTF8.GetString(bytes.Slice(4, 4));
            Assert.AreEqual(expectedProtocolName, actualProtocolName);

            var expectedProtocolVersion = 0x4;
            var actualProtocolVersion = bytes[8];
            Assert.AreEqual(expectedProtocolVersion, actualProtocolVersion);
        }

        [TestMethod]
        public void SetKeepAliveBytes_0x0078_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            Assert.AreEqual(0x00, bytes[10]);
            Assert.AreEqual(0x78, bytes[11]);
        }

        [TestMethod]
        public void SetKeepAliveBytes_0x0e10_GivenMessageWith_KeepAlive_3600()
        {
            var bytes = new ConnectMessage("") {KeepAlive = 3600}.GetBytes().Span;

            Assert.AreEqual(0x0e, bytes[12]);
            Assert.AreEqual(0x10, bytes[13]);
        }

        [TestMethod]
        public void EncodeClientId_TestClientId_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedClientIdLength = 12;
            var actualClientIdLength = ReadUInt16BigEndian(bytes.Slice(12));
            Assert.AreEqual(expectedClientIdLength, actualClientIdLength);

            var expectedClientId = "TestClientId";
            var actualClientId = UTF8.GetString(bytes.Slice(14, expectedClientIdLength));
            Assert.AreEqual(expectedClientId, actualClientId);
        }

        [TestMethod]
        public void EncodeWillTopic_TestWillTopic_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedWillTopicLength = 13;
            var actualWillTopicLength = ReadUInt16BigEndian(bytes.Slice(26));
            Assert.AreEqual(expectedWillTopicLength, actualWillTopicLength);

            var expectedWillTopic = "TestWillTopic";
            var actualWillTopic = UTF8.GetString(bytes.Slice(28, expectedWillTopicLength));
            Assert.AreEqual(expectedWillTopic, actualWillTopic);
        }

        [TestMethod]
        public void EncodeWillMessage_TestWillMessage_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedWillMessageLength = 15;
            var actualWillMessageLength = ReadUInt16BigEndian(bytes.Slice(41));
            Assert.AreEqual(expectedWillMessageLength, actualWillMessageLength);

            var expectedWillMessage = "TestWillMessage";
            var actualWillMessage = UTF8.GetString(bytes.Slice(43, expectedWillMessageLength));
            Assert.AreEqual(expectedWillMessage, actualWillMessage);
        }

        [TestMethod]
        public void EncodeUserName_TestUser_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedUserNameLength = 8;
            var actualUserNameLength = ReadUInt16BigEndian(bytes.Slice(58));
            Assert.AreEqual(expectedUserNameLength, actualUserNameLength);

            var expectedUserName = "TestUser";
            var actualUserName = UTF8.GetString(bytes.Slice(60, expectedUserNameLength));
            Assert.AreEqual(expectedUserName, actualUserName);
        }

        [TestMethod]
        public void EncodePassword_TestPassword_GivenSampleMessage()
        {
            var bytes = sampleMessage.GetBytes().Span;

            var expectedPasswordLength = 12;
            var actualPasswordLength = ReadUInt16BigEndian(bytes.Slice(68));
            Assert.AreEqual(expectedPasswordLength, actualPasswordLength);

            var expectedPassword = "TestPassword";
            var actualPassword = UTF8.GetString(bytes.Slice(70, expectedPasswordLength));
            Assert.AreEqual(expectedPassword, actualPassword);
        }

        [TestMethod]
        public void SetCleanSessionFlag_GivenMessageWith_CleanSession_True()
        {
            var m = new ConnectMessage("test-client-id") {CleanSession = true};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0010;
            var actual = bytes[11] & 0b0000_0010;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetCleanSessionFlag_GivenMessageWith_CleanSession_False()
        {
            var m = new ConnectMessage("test-client-id") {CleanSession = false};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[11] & 0b0000_0010;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillRetainFlag_GivenMessageWith_LastWillRetain_True()
        {
            var m = new ConnectMessage("test-client-id") {WillRetain = true};
            var bytes = m.GetBytes().Span;
            var expected = 0b0010_0000;
            var actual = bytes[11] & 0b0010_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetLastWillRetainFlag_GivenMessageWith_LastWillRetain_False()
        {
            var m = new ConnectMessage("test-client-id") {WillRetain = false};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[11] & 0b0010_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b00_GivenMessageWith_LastWillQoS_AtMostOnce()
        {
            var m = new ConnectMessage("test-client-id") {WillQoS = QoSLevel.AtMostOnce};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[11] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b01_GivenMessageWith_LastWillQoS_AtLeastOnce()
        {
            var m = new ConnectMessage("test-client-id") {WillQoS = QoSLevel.AtLeastOnce};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_1000;
            var actual = bytes[11] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b10_GivenMessageWith_LastWillQoS_ExactlyOnce()
        {
            var m = new ConnectMessage("test-client-id") {WillQoS = QoSLevel.ExactlyOnce};
            var bytes = m.GetBytes().Span;
            var expected = 0b0001_0000;
            var actual = bytes[11] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillPresentFlag_GivenMessageWithLastWillMessage_NotEmpty()
        {
            var m = new ConnectMessage("test-client-id") {WillMessage = UTF8.GetBytes("last-will-message")};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0100;
            var actual = bytes[11] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetLastWillPresentFlag_GivenMessageWithLastWillMessage_Null()
        {
            var m = new ConnectMessage("test-client-id") {WillMessage = null};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[11] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetUserNamePresentFlag_GivenMessageWithUserName_NotEmpty()
        {
            var m = new ConnectMessage("test-client-id") {UserName = "TestUser"};
            var bytes = m.GetBytes().Span;
            var expected = 0b1000_0000;
            var actual = bytes[11] & 0b1000_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetUserNamePresentFlag_GivenMessageWithUserName_Null()
        {
            var m = new ConnectMessage("test-client-id") {UserName = null};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[11] & 0b1000_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetPasswordPresentFlag_GivenMessageWithPassword_NotEmpty()
        {
            var m = new ConnectMessage("test-client-id") {Password = "TestPassword"};
            var bytes = m.GetBytes().Span;
            var expected = 0b0100_0000;
            var actual = bytes[11] & 0b0100_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetPasswordPresentFlag_GivenMessageWithPassword_Null()
        {
            var m = new ConnectMessage("test-client-id") {Password = null};
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[11] & 0b0100_0000;
            Assert.AreEqual(expected, actual);
        }
    }
}