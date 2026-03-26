namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState5;

[TestClass]
public class UnsubscribeShould
{

    [TestMethod]
    public void ReturnResultCodes_RemoveSubscriptions_GivenExistingFilters()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        var filters = new List<(byte[] Filter, byte Flags)>
        {
            ("testtopic1/#"u8.ToArray(), 0x00),
            ("testtopic2/#"u8.ToArray(), 0x01),
            ("testtopic3/#"u8.ToArray(), 0x02)
        };

        target.Subscribe(filters, 0);

        // Act
        List<byte[]> filtersToRemove = [
            "testtopic1/#"u8.ToArray(),
            "testtopic2/#"u8.ToArray()
        ];
        var actual = target.Unsubscribe(filtersToRemove, out var currentCount);

        // Assert
        Assert.AreEqual(1, currentCount);
        Assert.HasCount(2, actual);
        Assert.AreEqual(0x00, actual[0]);
        Assert.AreEqual(0x00, actual[1]);
    }

    [TestMethod]
    public void ReturnNotFoundResultCodes_GivenNonExistingFilters()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        var filters = new List<(byte[] Filter, byte Flags)>
        {
            ("testtopic1/#"u8.ToArray(), 0x00),
            ("testtopic2/#"u8.ToArray(), 0x01),
            ("testtopic3/#"u8.ToArray(), 0x02)
        };

        target.Subscribe(filters, 0);

        // Act
        List<byte[]> filtersToRemove = [
            "testtopic4/#"u8.ToArray(),
            "testtopic5/#"u8.ToArray()
        ];
        var actual = target.Unsubscribe(filtersToRemove, out var currentCount);

        // Assert
        Assert.AreEqual(3, currentCount);
        Assert.HasCount(2, actual);
        Assert.AreEqual(0x11, actual[0]);
        Assert.AreEqual(0x11, actual[1]);
    }
}