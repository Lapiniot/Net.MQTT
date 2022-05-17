using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class GetPayloadSizeShould
{
    [TestMethod]
    public void Return2GivenMessageWithEmptyClientId()
    {
        var m = new Packets.ConnectPacket(ReadOnlyMemory<byte>.Empty, 0x04, "MQTT"U8);
        const int expected = 2;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return16GivenMessageWithDefaultOptions()
    {
        var m = new Packets.ConnectPacket("test-client-id"U8, 0x04, "MQTT"U8);
        const int expected = 16;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return40GivenMessageWithTestUserAndTestPassword()
    {
        var m = new Packets.ConnectPacket("test-client-id"U8, 0x04, "MQTT"U8,
            userName: "TestUser"U8, password: "TestPassword"U8);
        const int expected = 40;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return50GivenMessageWithLastWillMessage()
    {
        var m = new Packets.ConnectPacket("test-client-id"U8, 0x04, "MQTT"U8, willTopic: "last/will/abc"U8, willMessage: "last-will-packet"U8);
        const int expected = 49;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }
}