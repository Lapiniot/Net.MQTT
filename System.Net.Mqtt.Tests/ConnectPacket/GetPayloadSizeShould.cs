using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class GetPayloadSizeShould
{
    [TestMethod]
    public void Return2GivenMessageWithEmptyClientId()
    {
        var m = new Packets.ConnectPacket("", 0x04, "MQTT");
        const int expected = 2;
        var actual = m.GetPayloadSize();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return0GivenMessageWithNullClientId()
    {
        var m = new Packets.ConnectPacket(null, 0x04, "MQTT");
        const int expected = 2;
        var actual = m.GetPayloadSize();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return16GivenMessageWithDefaultOptions()
    {
        var m = new Packets.ConnectPacket("test-client-id", 0x04, "MQTT");
        const int expected = 16;
        var actual = m.GetPayloadSize();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return40GivenMessageWithTestUserAndTestPassword()
    {
        var m = new Packets.ConnectPacket("test-client-id", 0x04, "MQTT", userName: "TestUser", password: "TestPassword");
        const int expected = 40;
        var actual = m.GetPayloadSize();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return50GivenMessageWithLastWillMessage()
    {
        var m = new Packets.ConnectPacket("test-client-id", 0x04, "MQTT", willTopic: "last/will/abc", willMessage: UTF8.GetBytes("last-will-packet"));
        const int expected = 49;
        var actual = m.GetPayloadSize();
        Assert.AreEqual(expected, actual);
    }
}