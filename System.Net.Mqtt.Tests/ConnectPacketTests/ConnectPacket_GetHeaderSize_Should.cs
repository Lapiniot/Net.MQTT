using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.ConnectPacketTests
{
    [TestClass]
    public class ConnectPacket_GetHeaderSize_Should
    {
        [TestMethod]
        public void Return12_GivenMessageWithProtocolV3()
        {
            var m = new ConnectPacket("test-client-id", 0x03, "MQIsdp");
            var expected = 12;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return10_GivenMessageWithProtocolV4()
        {
            var m = new ConnectPacket("test-client-id", 0x04, "MQTT");
            var expected = 10;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }
    }
}