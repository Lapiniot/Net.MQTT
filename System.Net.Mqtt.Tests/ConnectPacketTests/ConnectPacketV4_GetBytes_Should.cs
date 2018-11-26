using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.ConnectPacketTests
{
    [TestClass]
    public class ConnectPacketV4_GetBytes_Should
    {
        private readonly ConnectPacketV4 samplePacket = new ConnectPacketV4("TestClientId",
            userName: "TestUser", password: "TestPassword",
            willTopic: "TestWillTopic", willMessage: Encoding.UTF8.GetBytes("TestWillMessage"));

        [TestMethod]
        public void SetHeaderBytes_16_80_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

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
            var bytes = samplePacket.GetBytes().Span;

            var expectedProtocolNameLength = 4;
            var actualProtocolNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedProtocolNameLength, actualProtocolNameLength);

            var expectedProtocolName = "MQTT";
            var actualProtocolName = Encoding.UTF8.GetString(bytes.Slice(4, 4));
            Assert.AreEqual(expectedProtocolName, actualProtocolName);

            var expectedProtocolVersion = 0x4;
            var actualProtocolVersion = bytes[8];
            Assert.AreEqual(expectedProtocolVersion, actualProtocolVersion);
        }

        [TestMethod]
        public void SetKeepAliveBytes_0x0e10_GivenMessageWith_KeepAlive_3600()
        {
            var bytes = new ConnectPacketV4(null, 3600).GetBytes().Span;

            Assert.AreEqual(0x0e, bytes[10]);
            Assert.AreEqual(0x10, bytes[11]);
        }

        [TestMethod]
        public void EncodeClientId_TestClientId_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedClientIdLength = 12;
            var actualClientIdLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(12));
            Assert.AreEqual(expectedClientIdLength, actualClientIdLength);

            var expectedClientId = "TestClientId";
            var actualClientId = Encoding.UTF8.GetString(bytes.Slice(14, expectedClientIdLength));
            Assert.AreEqual(expectedClientId, actualClientId);
        }

        [TestMethod]
        public void EncodeWillTopic_TestWillTopic_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedWillTopicLength = 13;
            var actualWillTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(26));
            Assert.AreEqual(expectedWillTopicLength, actualWillTopicLength);

            var expectedWillTopic = "TestWillTopic";
            var actualWillTopic = Encoding.UTF8.GetString(bytes.Slice(28, expectedWillTopicLength));
            Assert.AreEqual(expectedWillTopic, actualWillTopic);
        }

        [TestMethod]
        public void EncodeWillMessage_TestWillMessage_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedWillMessageLength = 15;
            var actualWillMessageLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(41));
            Assert.AreEqual(expectedWillMessageLength, actualWillMessageLength);

            var expectedWillMessage = "TestWillMessage";
            var actualWillMessage = Encoding.UTF8.GetString(bytes.Slice(43, expectedWillMessageLength));
            Assert.AreEqual(expectedWillMessage, actualWillMessage);
        }

        [TestMethod]
        public void EncodeUserName_TestUser_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedUserNameLength = 8;
            var actualUserNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(58));
            Assert.AreEqual(expectedUserNameLength, actualUserNameLength);

            var expectedUserName = "TestUser";
            var actualUserName = Encoding.UTF8.GetString(bytes.Slice(60, expectedUserNameLength));
            Assert.AreEqual(expectedUserName, actualUserName);
        }

        [TestMethod]
        public void EncodePassword_TestPassword_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedPasswordLength = 12;
            var actualPasswordLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(68));
            Assert.AreEqual(expectedPasswordLength, actualPasswordLength);

            var expectedPassword = "TestPassword";
            var actualPassword = Encoding.UTF8.GetString(bytes.Slice(70, expectedPasswordLength));
            Assert.AreEqual(expectedPassword, actualPassword);
        }

        [TestMethod]
        public void SetCleanSessionFlag_GivenMessageWith_CleanSession_True()
        {
            var m = new ConnectPacketV4("test-client-id");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0010;
            var actual = bytes[9] & 0b0000_0010;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetCleanSessionFlag_GivenMessageWith_CleanSession_False()
        {
            var m = new ConnectPacketV4("test-client-id", cleanSession: false);
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0010;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillRetainFlag_GivenMessageWith_LastWillRetain_True()
        {
            var m = new ConnectPacketV4("test-client-id", willRetain: true);
            var bytes = m.GetBytes().Span;
            var expected = 0b0010_0000;
            var actual = bytes[9] & 0b0010_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetLastWillRetainFlag_GivenMessageWith_LastWillRetain_False()
        {
            var m = new ConnectPacketV4("test-client-id");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0010_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b00_GivenMessageWith_LastWillQoS_AtMostOnce()
        {
            var m = new ConnectPacketV4("test-client-id");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b01_GivenMessageWith_LastWillQoS_AtLeastOnce()
        {
            var m = new ConnectPacketV4("test-client-id", willQoS: 1);
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_1000;
            var actual = bytes[9] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b10_GivenMessageWith_LastWillQoS_ExactlyOnce()
        {
            var m = new ConnectPacketV4("test-client-id", willQoS: 2);
            var bytes = m.GetBytes().Span;
            var expected = 0b0001_0000;
            var actual = bytes[9] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotSetLastWillPresentFlag_GivenMessageWithLastWillTopic_Null()
        {
            var m = new ConnectPacketV4("test-client-id");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotSetLastWillPresentFlag_GivenMessageWithLastWillTopic_Empty()
        {
            var m = new ConnectPacketV4("test-client-id", willTopic: string.Empty);
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotSetLastWillPresentFlag_GivenMessageWithLastWillMessageOnly()
        {
            var m = new ConnectPacketV4("test-client-id", willMessage: Encoding.UTF8.GetBytes("last-will-packet"));
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillPresentFlag_GivenMessageWithLastWillTopic_NotEmpty()
        {
            var m = new ConnectPacketV4("test-client-id", willTopic: "last/will/topic");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0100;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EncodeZeroBytesMessage_GivenMessageWithLastWillTopicOnly()
        {
            var m = new ConnectPacketV4("test-client-id", willTopic: "last/will/topic");
            var bytes = m.GetBytes().Span;
            Assert.AreEqual(47, bytes.Length);
            Assert.AreEqual(0, bytes[45]);
            Assert.AreEqual(0, bytes[46]);
        }

        [TestMethod]
        public void SetUserNamePresentFlag_GivenMessageWithUserName_NotEmpty()
        {
            var m = new ConnectPacketV4("test-client-id", userName: "TestUser");
            var bytes = m.GetBytes().Span;
            var expected = 0b1000_0000;
            var actual = bytes[9] & 0b1000_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetUserNamePresentFlag_GivenMessageWithUserName_Null()
        {
            var m = new ConnectPacketV4("test-client-id");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b1000_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetPasswordPresentFlag_GivenMessageWithPassword_NotEmpty()
        {
            var m = new ConnectPacketV4("test-client-id", password: "TestPassword");
            var bytes = m.GetBytes().Span;
            var expected = 0b0100_0000;
            var actual = bytes[9] & 0b0100_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetPasswordPresentFlag_GivenMessageWithPassword_Null()
        {
            var m = new ConnectPacketV4("test-client-id");
            var bytes = m.GetBytes().Span;
            var expected = 0b0000_0000;
            var actual = bytes[9] & 0b0100_0000;
            Assert.AreEqual(expected, actual);
        }
    }
}