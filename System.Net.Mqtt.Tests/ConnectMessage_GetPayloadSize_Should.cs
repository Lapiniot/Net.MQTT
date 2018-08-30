using System.Net.Mqtt.Messages;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class ConnectMessage_GetPayloadSize_Should
    {
        [TestMethod]
        public void Return0_GivenMessageWithEmptyClientId()
        {
            var m = new ConnectMessage("");
            var expected = 0;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return0_GivenMessageWithNullClientId()
        {
            var m = new ConnectMessage(null);
            var expected = 0;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return16_GivenMessageWithDefaultOptions()
        {
            var m = new ConnectMessage("test-client-id");
            var expected = 16;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return40_GivenMessageWith_TestUser_And_TestPassword()
        {
            var m = new ConnectMessage("test-client-id") {UserName = "TestUser", Password = "TestPassword"};
            var expected = 40;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return50_GivenMessageWith_LastWillMessage()
        {
            var m = new ConnectMessage("test-client-id") {WillTopic = "last/will/abc", WillMessage = Encoding.UTF8.GetBytes("last-will-message")};
            var expected = 50;
            var actual = m.GetPayloadSize();
            Assert.AreEqual(expected, actual);
        }
    }
}