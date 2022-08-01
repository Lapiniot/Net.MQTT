using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class GetPayloadSizeShould
{
    [TestMethod]
    public void Return2GivenMessageWithEmptyClientId()
    {
        var m = new Packets.ConnectPacket(ReadOnlyMemory<byte>.Empty, 0x04, "MQTT"u8.ToArray());
        const int expected = 2;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return16GivenMessageWithDefaultOptions()
    {
        var m = new Packets.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray());
        const int expected = 16;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return40GivenMessageWithTestUserAndTestPassword()
    {
        var m = new Packets.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(),
            userName: "TestUser"u8.ToArray(), password: "TestPassword"u8.ToArray());
        const int expected = 40;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return50GivenMessageWithLastWillMessage()
    {
        var m = new Packets.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), willTopic: "last/will/abc"u8.ToArray(), willMessage: "last-will-packet"u8.ToArray());
        const int expected = 49;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }
}