using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class ConnectPacket_GetHeaderSize_Should
    {
        [TestMethod]
        public void Return12_GivenMessageWithProtocolName_MQIsdp()
        {
            var m = new ConnectPacket("test-client-id");
            var expected = 12;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return10_GivenMessageWithProtocolName_MQTT()
        {
            var m = new ConnectPacket("test-client-id") {ProtocolName = "MQTT"};
            var expected = 10;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }
    }
}