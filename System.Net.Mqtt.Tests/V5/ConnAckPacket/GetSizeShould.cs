using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.ConnAckPacket;

[TestClass]
public class GetSizeShould
{
    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenPacketWithDefaultPropValues()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true);

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(5, actual);
        Assert.AreEqual(3, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultSessionExpiryInterval()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { SessionExpiryInterval = 300 };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(10, actual);
        Assert.AreEqual(8, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultReceiveMaximum()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { ReceiveMaximum = 1024 };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(8, actual);
        Assert.AreEqual(6, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultMaximumQoS()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { MaximumQoS = QoSLevel.QoS1 };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(7, actual);
        Assert.AreEqual(5, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultRetainAvailable()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { RetainAvailable = false };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(7, actual);
        Assert.AreEqual(5, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultWildcardSubscriptionAvailable()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { WildcardSubscriptionAvailable = false };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(7, actual);
        Assert.AreEqual(5, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultSubscriptionIdentifiersAvailable()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { SubscriptionIdentifiersAvailable = false };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(7, actual);
        Assert.AreEqual(5, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultSharedSubscriptionAvailable()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { SharedSubscriptionAvailable = false };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(7, actual);
        Assert.AreEqual(5, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultMaximumPacketSize()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { MaximumPacketSize = 4096 };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(10, actual);
        Assert.AreEqual(8, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultServerKeepAlive()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { ServerKeepAlive = 120 };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(8, actual);
        Assert.AreEqual(6, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultAssignedClientId()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { AssignedClientId = "mqttx_a6438c55"u8.ToArray() };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(22, actual);
        Assert.AreEqual(20, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultTopicAliasMaximum()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { TopicAliasMaximum = 1024 };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(8, actual);
        Assert.AreEqual(6, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultReasonString()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { ReasonString = "Invalid client id"u8.ToArray() };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(25, actual);
        Assert.AreEqual(23, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultResponseInfo()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { ResponseInfo = "Response info"u8.ToArray() };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(21, actual);
        Assert.AreEqual(19, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultServerReference()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { ServerReference = "Server #1"u8.ToArray() };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(17, actual);
        Assert.AreEqual(15, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultAuthMethod()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { AuthMethod = "Bearer"u8.ToArray() };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(14, actual);
        Assert.AreEqual(12, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultAuthData()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true) { AuthData = "72f26a1a-337ac7cf"u8.ToArray() };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(25, actual);
        Assert.AreEqual(23, remainingLength);
    }

    [TestMethod]
    public void ReturnSizeAndRemainingLength_GivenNonDefaultProperties()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true)
        {
            Properties = new List<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>()
            {
                new("user-prop-1"u8.ToArray(),"user-prop1-value"u8.ToArray()),
                new("user-prop-2"u8.ToArray(),"user-prop2-value"u8.ToArray())
            }
        };

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(69, actual);
        Assert.AreEqual(67, remainingLength);
    }
}