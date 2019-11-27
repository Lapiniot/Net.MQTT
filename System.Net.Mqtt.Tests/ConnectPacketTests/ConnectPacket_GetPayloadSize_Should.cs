using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests.ConnectPacketTests
{
    [TestClass]
    public class ConnectPacket_GetPayloadSize_Should
    {
        [TestMethod]
        public void Return2_GivenMessageWithEmptyClientId()
        {
            var m = new ConnectPacket("", 0x04, "MQTT");
            const int expected = 2;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return0_GivenMessageWithNullClientId()
        {
            var m = new ConnectPacket(null, 0x04, "MQTT");
            const int expected = 2;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return16_GivenMessageWithDefaultOptions()
        {
            var m = new ConnectPacket("test-client-id", 0x04, "MQTT");
            const int expected = 16;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return40_GivenMessageWith_TestUser_And_TestPassword()
        {
            var m = new ConnectPacket("test-client-id", 0x04, "MQTT", userName: "TestUser", password: "TestPassword");
            const int expected = 40;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return50_GivenMessageWith_LastWillMessage()
        {
            var m = new ConnectPacket("test-client-id", 0x04, "MQTT", willTopic: "last/will/abc", willMessage: UTF8.GetBytes("last-will-packet"));
            const int expected = 49;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }
    }
}