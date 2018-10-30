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
            var m = new ConnectPacketV3("test-client-id");
            var expected = 12;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Return10_GivenMessageWithProtocolV4()
        {
            var m = new ConnectPacketV4("test-client-id");
            var expected = 10;
            var actual = m.GetHeaderSize();
            Assert.AreEqual(expected, actual);
        }
    }
}