namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState4;

[TestClass]
public class SubscribeShould
{
    [TestMethod]
    public void ReturnResultCodes_RejectSubscriptions_GivenInvalidFilters()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState4();

        // Act
        var feedback = state.Subscribe([
            ("#"u8.ToArray(), 0x03),       // QoS 3 is invalid
            ("#/1"u8.ToArray(), 0x00),     // Topic is invalid
        ], out var currentCount);

        // Assert
        Assert.HasCount(2, feedback);
        Assert.AreEqual(0, currentCount);
        CollectionAssert.AreEqual([(byte)0x80, (byte)0x80], feedback);
    }
}