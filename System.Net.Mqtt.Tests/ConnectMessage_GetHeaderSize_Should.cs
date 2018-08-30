using System.Net.Mqtt.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class ConnectMessage_GetHeaderSize_Should
    {
        [TestMethod]
        public void Return12_GivenMessageWithProtocolName_MQIsdp()
        {
            var m = new ConnectMessage("test-client-id");
            var expected = 12;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return10_GivenMessageWithProtocolName_MQTT()
        {
            var m = new ConnectMessage("test-client-id") {ProtocolName = "MQTT"};
            var expected = 10;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }
    }
}