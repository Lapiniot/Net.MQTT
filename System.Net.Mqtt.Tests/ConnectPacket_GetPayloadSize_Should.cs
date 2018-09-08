using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class ConnectPacket_GetPayloadSize_Should
    {
        [TestMethod]
        public void Return0_GivenMessageWithEmptyClientId()
        {
            var m = new ConnectPacket("");
            var expected = 0;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return0_GivenMessageWithNullClientId()
        {
            var m = new ConnectPacket(null);
            var expected = 0;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return16_GivenMessageWithDefaultOptions()
        {
            var m = new ConnectPacket("test-client-id");
            var expected = 16;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return40_GivenMessageWith_TestUser_And_TestPassword()
        {
            var m = new ConnectPacket("test-client-id") {UserName = "TestUser", Password = "TestPassword"};
            var expected = 40;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return50_GivenMessageWith_LastWillMessage()
        {
            var m = new ConnectPacket("test-client-id") {WillTopic = "last/will/abc", WillMessage = Encoding.UTF8.GetBytes("last-will-packet")};
            var expected = 49;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }
    }
}