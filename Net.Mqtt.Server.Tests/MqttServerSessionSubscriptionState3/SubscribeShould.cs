namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState3;

[TestClass]
public class SubscribeShould
{
    [TestMethod]
    public void ReturnResultCodes_AddSubscriptions_GivenValidFilters()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();

        // Act
        var feedback = state.Subscribe([
            ("testtopic1/#"u8.ToArray(), 0x01), // QoS 1
            ("testtopic2/#"u8.ToArray(), 0x02)  // QoS 2
        ], out var currentCount);

        // Assert
        Assert.HasCount(2, feedback);
        Assert.AreEqual(2, currentCount);
        CollectionAssert.AreEqual([(byte)0x01, (byte)0x02], feedback);
    }

    [TestMethod]
    public void ReturnResultCodes_RejectSubscriptions_GivenInvalidFilters()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();

        // Act
        var feedback = state.Subscribe([
            ("#"u8.ToArray(), 0x03),       // QoS 3 is invalid
            ("#/1"u8.ToArray(), 0x00),     // Topic is invalid
        ], out var currentCount);

        // Assert
        Assert.HasCount(2, feedback);
        Assert.AreEqual(0, currentCount);
        CollectionAssert.AreEqual([(byte)0x03, (byte)0x00], feedback);
    }

    [TestMethod]
    public void UpdateExistingSubscription_GivenSameFilterButDifferentQoS()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();
        state.Subscribe([("test/topic/#"u8.ToArray(), 0x01)], out _); // First subscribe QoS 1

        // Act
        var feedback = state.Subscribe([("test/topic/#"u8.ToArray(), 0x02)], out var currentCount); // Upgrade QoS to 2

        // Assert
        Assert.HasCount(1, feedback);
        Assert.AreEqual(0x02, feedback[0]);     // Updated QoS flag
        Assert.AreEqual(1, currentCount);       // Count remains same
    }
}