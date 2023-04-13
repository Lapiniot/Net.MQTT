using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.ConnectPacket;

[TestClass]
public class GetHeaderSizeShould
{
    [TestMethod]
    public void Return12GivenMessageWithProtocolV3()
    {
        var m = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x03, "MQIsdp"u8.ToArray());
        const int expected = 12;
        var actual = m.HeaderSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return10GivenMessageWithProtocolV4()
    {
        var m = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray());
        const int expected = 10;
        var actual = m.HeaderSize;
        Assert.AreEqual(expected, actual);
    }
}