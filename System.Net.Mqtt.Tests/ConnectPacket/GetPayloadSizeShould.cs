using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class GetPayloadSizeShould
{
    [TestMethod]
    public void Return2GivenMessageWithEmptyClientId()
    {
        var m = new Packets.ConnectPacket(ReadOnlyMemory<byte>.Empty, 0x04, (byte[])"MQTT");
        const int expected = 2;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return16GivenMessageWithDefaultOptions()
    {
        var m = new Packets.ConnectPacket((byte[])"test-client-id", 0x04, (byte[])"MQTT");
        const int expected = 16;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return40GivenMessageWithTestUserAndTestPassword()
    {
        var m = new Packets.ConnectPacket((byte[])"test-client-id", 0x04, (byte[])"MQTT",
            userName: (byte[])"TestUser", password: (byte[])"TestPassword");
        const int expected = 40;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return50GivenMessageWithLastWillMessage()
    {
        var m = new Packets.ConnectPacket((byte[])"test-client-id", 0x04, (byte[])"MQTT", willTopic: (byte[])"last/will/abc", willMessage: (byte[])"last-will-packet");
        const int expected = 49;
        var actual = m.PayloadSize;
        Assert.AreEqual(expected, actual);
    }
}