using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Text.Encoding;

namespace System.Net.Mqtt.ConnectPacketTests
{
    [TestClass]
    public class ConnectPacket_GetPayloadSize_Should
    {
        [TestMethod]
        public void Return2_GivenMessageWithEmptyClientId()
        {
            var m = new ConnectPacketV4("");
            var expected = 2;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return0_GivenMessageWithNullClientId()
        {
            var m = new ConnectPacketV4();
            var expected = 2;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return16_GivenMessageWithDefaultOptions()
        {
            var m = new ConnectPacketV4("test-client-id");
            var expected = 16;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return40_GivenMessageWith_TestUser_And_TestPassword()
        {
            var m = new ConnectPacketV4("test-client-id", userName: "TestUser", password: "TestPassword");
            var expected = 40;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return50_GivenMessageWith_LastWillMessage()
        {
            var m = new ConnectPacketV4("test-client-id", willTopic: "last/will/abc", willMessage: UTF8.GetBytes("last-will-packet"));
            var expected = 49;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }
    }
}