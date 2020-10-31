using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacketTests
{
    [TestClass]
    public class ConnectPacketV4_Write_Should
    {
        private readonly ConnectPacket samplePacket =
            new ConnectPacket("TestClientId", 0x04, "MQTT", 120, true, "TestUser", "TestPassword",
                "TestWillTopic", Encoding.UTF8.GetBytes("TestWillMessage"));

        [TestMethod]
        public void SetHeaderBytes_16_80_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const byte expectedPacketType = 0b0001_0000;
            var actualPacketType = bytes[0];
            Assert.AreEqual(expectedPacketType, actualPacketType);

            const int expectedRemainingLength = 80;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void SetProtocolInfoBytes__GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const int expectedProtocolNameLength = 4;
            var actualProtocolNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            Assert.AreEqual(expectedProtocolNameLength, actualProtocolNameLength);

            const string expectedProtocolName = "MQTT";
            var actualProtocolName = Encoding.UTF8.GetString(bytes.Slice(4, 4));
            Assert.AreEqual(expectedProtocolName, actualProtocolName);

            const int expectedProtocolVersion = 0x4;
            var actualProtocolVersion = bytes[8];
            Assert.AreEqual(expectedProtocolVersion, actualProtocolVersion);
        }

        [TestMethod]
        public void SetKeepAliveBytes_0x0e10_GivenMessageWith_KeepAlive_3600()
        {
            Span<byte> bytes = new byte[14];
            new ConnectPacket(null, 0x04, "MQTT", 3600).Write(bytes, 12);

            Assert.AreEqual(0x0e, bytes[10]);
            Assert.AreEqual(0x10, bytes[11]);
        }

        [TestMethod]
        public void EncodeClientId_TestClientId_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const int expectedClientIdLength = 12;
            var actualClientIdLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]);
            Assert.AreEqual(expectedClientIdLength, actualClientIdLength);

            const string expectedClientId = "TestClientId";
            var actualClientId = Encoding.UTF8.GetString(bytes.Slice(14, expectedClientIdLength));
            Assert.AreEqual(expectedClientId, actualClientId);
        }

        [TestMethod]
        public void EncodeWillTopic_TestWillTopic_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const int expectedWillTopicLength = 13;
            var actualWillTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[26..]);
            Assert.AreEqual(expectedWillTopicLength, actualWillTopicLength);

            const string expectedWillTopic = "TestWillTopic";
            var actualWillTopic = Encoding.UTF8.GetString(bytes.Slice(28, expectedWillTopicLength));
            Assert.AreEqual(expectedWillTopic, actualWillTopic);
        }

        [TestMethod]
        public void EncodeWillMessage_TestWillMessage_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const int expectedWillMessageLength = 15;
            var actualWillMessageLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[41..]);
            Assert.AreEqual(expectedWillMessageLength, actualWillMessageLength);

            const string expectedWillMessage = "TestWillMessage";
            var actualWillMessage = Encoding.UTF8.GetString(bytes.Slice(43, expectedWillMessageLength));
            Assert.AreEqual(expectedWillMessage, actualWillMessage);
        }

        [TestMethod]
        public void EncodeUserName_TestUser_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const int expectedUserNameLength = 8;
            var actualUserNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[58..]);
            Assert.AreEqual(expectedUserNameLength, actualUserNameLength);

            const string expectedUserName = "TestUser";
            var actualUserName = Encoding.UTF8.GetString(bytes.Slice(60, expectedUserNameLength));
            Assert.AreEqual(expectedUserName, actualUserName);
        }

        [TestMethod]
        public void EncodePassword_TestPassword_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[82];
            samplePacket.Write(bytes, 80);

            const int expectedPasswordLength = 12;
            var actualPasswordLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[68..]);
            Assert.AreEqual(expectedPasswordLength, actualPasswordLength);

            const string expectedPassword = "TestPassword";
            var actualPassword = Encoding.UTF8.GetString(bytes.Slice(70, expectedPasswordLength));
            Assert.AreEqual(expectedPassword, actualPassword);
        }

        [TestMethod]
        public void SetCleanSessionFlag_GivenMessageWith_CleanSession_True()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT").Write(bytes, 26);

            const int expected = 0b0000_0010;
            var actual = bytes[9] & 0b0000_0010;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetCleanSessionFlag_GivenMessageWith_CleanSession_False()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT", cleanSession: false).Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0010;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillRetainFlag_GivenMessageWith_LastWillRetain_True()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willRetain: true).Write(bytes, 28);

            const int expected = 0b0010_0000;
            var actual = bytes[9] & 0b0010_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetLastWillRetainFlag_GivenMessageWith_LastWillRetain_False()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT").Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0010_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b00_GivenMessageWith_LastWillQoS_AtMostOnce()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT").Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b01_GivenMessageWith_LastWillQoS_AtLeastOnce()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willQoS: 1).Write(bytes, 26);

            const int expected = 0b0000_1000;
            var actual = bytes[9] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillQoSFlags_0b10_GivenMessageWith_LastWillQoS_ExactlyOnce()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willQoS: 2).Write(bytes, 26);

            const int expected = 0b0001_0000;
            var actual = bytes[9] & 0b0001_1000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotSetLastWillPresentFlag_GivenMessageWithLastWillTopic_Null()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT").Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotSetLastWillPresentFlag_GivenMessageWithLastWillTopic_Empty()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willTopic: string.Empty).Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotSetLastWillPresentFlag_GivenMessageWithLastWillMessageOnly()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willMessage: Encoding.UTF8.GetBytes("last-will-packet")).Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetLastWillPresentFlag_GivenMessageWithLastWillTopic_NotEmpty()
        {
            Span<byte> bytes = new byte[47];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willTopic: "last/will/topic").Write(bytes, 45);

            const int expected = 0b0000_0100;
            var actual = bytes[9] & 0b0000_0100;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EncodeZeroBytesMessage_GivenMessageWithLastWillTopicOnly()
        {
            Span<byte> bytes = new byte[47];
            new ConnectPacket("test-client-id", 0x04, "MQTT", willTopic: "last/will/topic").Write(bytes, 45);

            Assert.AreEqual(47, bytes.Length);
            Assert.AreEqual(0, bytes[45]);
            Assert.AreEqual(0, bytes[46]);
        }

        [TestMethod]
        public void SetUserNamePresentFlag_GivenMessageWithUserName_NotEmpty()
        {
            Span<byte> bytes = new byte[38];
            new ConnectPacket("test-client-id", 0x04, "MQTT", userName: "TestUser").Write(bytes, 36);

            const int expected = 0b1000_0000;
            var actual = bytes[9] & 0b1000_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetUserNamePresentFlag_GivenMessageWithUserName_Null()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT").Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b1000_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetPasswordPresentFlag_GivenMessageWithPassword_NotEmpty()
        {
            Span<byte> bytes = new byte[42];
            new ConnectPacket("test-client-id", 0x04, "MQTT", password: "TestPassword").Write(bytes, 40);

            const int expected = 0b0100_0000;
            var actual = bytes[9] & 0b0100_0000;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ResetPasswordPresentFlag_GivenMessageWithPassword_Null()
        {
            Span<byte> bytes = new byte[28];
            new ConnectPacket("test-client-id", 0x04, "MQTT").Write(bytes, 26);

            const int expected = 0b0000_0000;
            var actual = bytes[9] & 0b0100_0000;
            Assert.AreEqual(expected, actual);
        }
    }
}