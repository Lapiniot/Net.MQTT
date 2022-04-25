using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class GetPayloadSizeShould
{
    [TestMethod]
    public void Return2GivenMessageWithEmptyClientId()
    {
        var m = new Packets.ConnectPacket(ReadOnlyMemory<byte>.Empty, 0x04, UTF8.GetBytes("MQTT"));
        const int expected = 2;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return16GivenMessageWithDefaultOptions()
    {
        var m = new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"));
        const int expected = 16;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return40GivenMessageWithTestUserAndTestPassword()
    {
        var m = new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"),
            userName: UTF8.GetBytes("TestUser"), password: UTF8.GetBytes("TestPassword"));
        const int expected = 40;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return50GivenMessageWithLastWillMessage()
    {
        var m = new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), willTopic: UTF8.GetBytes("last/will/abc"), willMessage: UTF8.GetBytes("last-will-packet"));
        const int expected = 49;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }
}